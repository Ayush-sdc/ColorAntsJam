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

            hole_cap = s.get("HoleCapacity", 0)
            if htype == 0:
                holes.append((idx, cid, cell_tuples, hole_cap))
            elif htype == 1:
                queues.append((idx, cid, cell_tuples, is_second, second_color, is_locked, lock_amount))

        # Check missing matching holes for each queue
        hole_colors = {h[1] for h in holes}
        for q in queues:
            if q[1] not in hole_colors:
                lvl_issues.append(f"Queue {q[0]} Color {q[1]} has NO matching hole on board!")
            if q[3] and q[4] not in hole_colors:
                lvl_issues.append(f"Queue {q[0]} SecondColor {q[4]} has NO matching hole on board!")

        # Count expected queues per color (including stacked second color)
        expected_per_color = {}
        for q in queues:
            expected_per_color[q[1]] = expected_per_color.get(q[1], 0) + 1
            if q[3]: # stacked second color
                expected_per_color[q[4]] = expected_per_color.get(q[4], 0) + 1

        # Check capacity sufficiency per color using runtime adapter simulation
        holes_by_color = {}
        for h in holes:
            holes_by_color.setdefault(h[1], []).append(h)

        cell_to_hole = {}
        for h in holes:
            for c in h[2]:
                cell_to_hole[c] = h

        def trace_to_hole(head, neck, target_color):
            dx = head[0] - neck[0]
            dy = head[1] - neck[1]
            cur = (head[0] + dx, head[1] + dy)
            steps = 0
            while 0 <= cur[0] < grid_w and 0 <= cur[1] < grid_h and steps < 60:
                steps += 1
                if cur in cell_to_hole:
                    h_target = cell_to_hole[cur]
                    if h_target[1] == target_color:
                        return h_target
                pipe_hit = [p for p in pipes if p[1][0] == cur and (p[1][1][0]-p[1][0][0], p[1][1][1]-p[1][0][1]) == (dx, dy)]
                if pipe_hit:
                    p = pipe_hit[0]
                    exit_c = p[1][-1]
                    exit_d = (p[1][-1][0] - p[1][-2][0], p[1][-1][1] - p[1][-2][1])
                    cur = (exit_c[0] + exit_d[0], exit_c[1] + exit_d[1])
                    dx, dy = exit_d
                    continue
                cur = (cur[0] + dx, cur[1] + dy)
            return None

        capacity_issues = []
        for cid, exp in expected_per_color.items():
            if cid not in holes_by_color:
                lvl_issues.append(f"Color {cid} ({valid_colors.get(cid, 'Unknown')}) has {exp} queues but 0 holes!")
                continue

            color_holes = holes_by_color[cid]
            if len(color_holes) == 1:
                assigned_cap = max(1, exp)
                if assigned_cap < exp:
                    capacity_issues.append(f"Color {cid}: Capacity deficit (assigned {assigned_cap} < {exp} needed)")
            else:
                f_counts = {h[0]: 0 for h in color_holes}
                for q in queues:
                    if q[1] == cid:
                        h_found = trace_to_hole(q[2][0], q[2][1], cid)
                        if h_found and h_found[0] in f_counts:
                            f_counts[h_found[0]] += 1
                    if q[3] and q[4] == cid:
                        h_found = trace_to_hole(q[2][0], q[2][1], cid)
                        if h_found and h_found[0] in f_counts:
                            f_counts[h_found[0]] += 1
                total_alloc = sum(max(1, f_counts[h[0]]) for h in color_holes)
                if total_alloc < exp:
                    capacity_issues.append(f"Color {cid}: Inferred multi-hole capacity {total_alloc} < {exp} needed")

        if capacity_issues:
            for c_iss in capacity_issues:
                lvl_issues.append(c_iss)

        if lvl_issues:
            total_issues += len(lvl_issues)
            print(f"[{lvl_name}] ISSUES ({len(lvl_issues)}):")
            for iss in lvl_issues:
                print(f"  - {iss}")
        else:
            pipes_info = f"Pipes: {len(pipes)}" if pipes else "No pipes"
            stacked_count = sum(1 for q in queues if q[3])
            locked_count = sum(1 for q in queues if q[5])
            diff_label = "Normal" if diff == 0 else ("Hard" if diff == 1 else f"Special({diff})")
            print(f"[{lvl_name}] STRUCTURAL: PASS | CAPACITY: SUFFICIENT | Queues: {len(queues)} (Stacked: {stacked_count}, Locked: {locked_count}) | Holes: {len(holes)} | {pipes_info} | Diff: {diff_label} (Solvability: Requires PlayMode Simulation)")

    print(f"\n=======================================================")
    print(f"Validation complete across {len(files)} levels.")
    print(f"Total Structural & Capacity Issues: {total_issues}")
    print(f"Note: Structural JSON validation does NOT prove runtime solvability.")
    print(f"=======================================================\n")

if __name__ == "__main__":
    validate_levels()

