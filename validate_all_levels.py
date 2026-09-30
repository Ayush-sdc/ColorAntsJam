import json
import os
import glob

LEVEL_DIR = r"c:\Users\ayush\Desktop\Unity Projects\Slither Ants Jam\Assets\Data\ReferenceLevelJSON"

def validate_levels():
    files = sorted(glob.glob(os.path.join(LEVEL_DIR, "Level_*.json")))
    print(f"Found {len(files)} level files.")

    report = []
    total_issues = 0

    valid_colors = {
        0: "Red", 1: "Yellow", 2: "PastelPink", 3: "Pink",
        4: "Orange", 5: "Green", 6: "Teal", 7: "Purple",
        8: "Blue", 9: "Cyan", 10: "DarkBlue", 11: "White",
        12: "Grey", 13: "Brown", 14: "Black"
    }

    for fpath in files:
        lvl_name = os.path.basename(fpath).replace(".json", "")
        with open(fpath, "r", encoding="utf-8") as f:
            data = json.load(f)

        grid_w = data.get("GridSize", {}).get("x", 20)
        grid_h = data.get("GridSize", {}).get("y", 20)
        snakes = data.get("snakes", [])
        diff = data.get("LevelDifficulty", 0)
        dur = data.get("Duration", 0)

        lvl_issues = []
        holes = []
        queues = []
        pipes = []

        all_claimed_cells = {}

        for idx, s in enumerate(snakes):
            htype = s.get("HolderType", 1)
            cid = s.get("ColorID", 0)
            cells = s.get("Cells", [])
            is_locked = s.get("IsLocked", False)
            lock_amount = s.get("LockAmount", 0)
            pipe_count = s.get("PipeCount", 0)
            is_second = s.get("IsSecondColor", False)
            second_color = s.get("SecondColor", 0)

            # 1. Invalid colors
            if htype != 2: # pipes can have ColorID 0 or ignored
                if cid not in valid_colors:
                    lvl_issues.append(f"Record {idx}: Invalid ColorID {cid}")
                if is_second and second_color not in valid_colors:
                    lvl_issues.append(f"Record {idx}: Invalid SecondColor {second_color}")

            # 2. Duplicate cells in body
            cell_tuples = [(c["x"], c["y"]) for c in cells]
            if len(cell_tuples) != len(set(cell_tuples)):
                lvl_issues.append(f"Record {idx}: Internal duplicate cells in body: {cell_tuples}")

            # 3. Out of bounds
            for c in cell_tuples:
                if c[0] < 0 or c[0] >= grid_w or c[1] < 0 or c[1] >= grid_h:
                    lvl_issues.append(f"Record {idx}: Out of bounds cell {c} for grid {grid_w}x{grid_h}")

            # 4. Disconnected queue bodies / pipes
            for i in range(1, len(cell_tuples)):
                c0 = cell_tuples[i-1]
                c1 = cell_tuples[i]
                manhattan = abs(c0[0] - c1[0]) + abs(c0[1] - c1[1])
                if manhattan != 1:
                    lvl_issues.append(f"Record {idx}: Disconnected segment between {c0} and {c1}")

            # 5. Invalid pipes
            if htype == 2:
                if len(cell_tuples) < 2:
                    lvl_issues.append(f"Record {idx}: Pipe has fewer than 2 cells ({len(cell_tuples)})")
                pipes.append((idx, cell_tuples, pipe_count))

            # 6. Negative lock / pipe values
            if lock_amount < 0:
                lvl_issues.append(f"Record {idx}: Negative LockAmount {lock_amount}")
            if pipe_count < 0:
                lvl_issues.append(f"Record {idx}: Negative PipeCount {pipe_count}")

            if htype == 0:
                holes.append((idx, cid, cell_tuples))
            elif htype == 1:
                queues.append((idx, cid, cell_tuples, is_second, second_color, is_locked, lock_amount))

        # Check missing matching holes for each queue
        hole_colors = {h[1] for h in holes}
        for q in queues:
            if q[1] not in hole_colors:
                lvl_issues.append(f"Queue {q[0]} Color {q[1]} has NO matching hole on board!")
            if q[3] and q[4] not in hole_colors:
                lvl_issues.append(f"Queue {q[0]} SecondColor {q[4]} has NO matching hole on board!")

        # Count expected queues per color
        expected_per_color = {}
        for q in queues:
            expected_per_color[q[1]] = expected_per_color.get(q[1], 0) + 1
            if q[3]: # stacked second color
                expected_per_color[q[4]] = expected_per_color.get(q[4], 0) + 1

        # Check capacity
        hole_count_per_color = {}
        for h in holes:
            hole_count_per_color[h[1]] = hole_count_per_color.get(h[1], 0) + 1

        for cid, exp in expected_per_color.items():
            if cid not in hole_count_per_color:
                lvl_issues.append(f"Color {cid} ({valid_colors.get(cid, 'Unknown')}) has {exp} queues but 0 holes!")

        if lvl_issues:
            total_issues += len(lvl_issues)
            print(f"[{lvl_name}] ISSUES ({len(lvl_issues)}):")
            for iss in lvl_issues:
                print(f"  - {iss}")
        else:
            pipes_info = f"Pipes: {len(pipes)}" if pipes else "No pipes"
            stacked_count = sum(1 for q in queues if q[3])
            locked_count = sum(1 for q in queues if q[5])
            print(f"[{lvl_name}] PASS | Queues: {len(queues)} (Stacked: {stacked_count}, Locked: {locked_count}) | Holes: {len(holes)} | {pipes_info} | Diff: {diff}")

    print(f"\nValidation complete. Total issues across 45 levels: {total_issues}")

if __name__ == "__main__":
    validate_levels()
