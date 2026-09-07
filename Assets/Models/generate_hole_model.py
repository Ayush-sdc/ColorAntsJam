import math

def generate_hole_obj(output_obj_path, output_mtl_path):
    # Overall Dimensions matching Unity scene and reference photo
    # Capsule width (outer x span) = 1.30, height (outer z span) = 0.52
    outer_w = 1.30
    outer_h = 0.52
    tube_r = 0.075      # Radius of the tubular cross-section
    tube_h = 0.085      # Height (half-thickness) of the tubular cross-section
    
    r_cap = (outer_h * 0.5) - tube_r
    r_inner = r_cap - tube_r
    straight_len = outer_w - 2.0 * (r_cap + tube_r)
    half_l = straight_len * 0.5
    
    # Grid resolutions
    path_res = 64
    cross_res = 16
    arc_res = 24
    
    straight_res = max(4, path_res // 8)
    arc_path_res = max(12, (path_res - 2 * straight_res) // 2)
    
    # -------------------------------------------------------------
    # 1. BUILD PATH FOR TUBULAR RIM
    # -------------------------------------------------------------
    path_pts = []
    path_norms = []
    
    # 1. Top straight (-half_l -> +half_l)
    for i in range(straight_res):
        t = i / float(straight_res)
        x = -half_l + t * (2.0 * half_l)
        path_pts.append((x, 0.0, r_cap))
        path_norms.append((0.0, 0.0, 1.0))
        
    # 2. Right semicircle (+pi/2 -> -pi/2)
    for i in range(arc_path_res):
        t = i / float(arc_path_res)
        ang = math.pi * 0.5 - t * math.pi
        x = half_l + r_cap * math.cos(ang)
        z = r_cap * math.sin(ang)
        nx = math.cos(ang)
        nz = math.sin(ang)
        path_pts.append((x, 0.0, z))
        path_norms.append((nx, 0.0, nz))
        
    # 3. Bottom straight (+half_l -> -half_l)
    for i in range(straight_res):
        t = i / float(straight_res)
        x = half_l - t * (2.0 * half_l)
        path_pts.append((x, 0.0, -r_cap))
        path_norms.append((0.0, 0.0, -1.0))
        
    # 4. Left semicircle (-pi/2 -> -3pi/2)
    for i in range(arc_path_res):
        t = i / float(arc_path_res)
        ang = -math.pi * 0.5 - t * math.pi
        x = -half_l + r_cap * math.cos(ang)
        z = r_cap * math.sin(ang)
        nx = math.cos(ang)
        nz = math.sin(ang)
        path_pts.append((x, 0.0, z))
        path_norms.append((nx, 0.0, nz))
        
    M = len(path_pts)
    K = cross_res
    
    verts = []
    normals = []
    uvs = []
    
    def add_vertex(pos, norm, uv):
        verts.append(pos)
        normals.append(norm)
        uvs.append(uv)
        return len(verts)
        
    groups = {
        "SlotRim": {"mtl": "Mat_Rim_Red", "faces": []},
        "SlotPit": {"mtl": "Mat_Pit_Dark", "faces": []},
        "SlotBadge": {"mtl": "Mat_Badge_Red", "faces": []},
        "NumberOne": {"mtl": "Mat_Number_White", "faces": []}
    }
    
    # Build Tube vertices
    tube_vert_indices = []
    for i in range(M):
        center_x, center_y, center_z = path_pts[i]
        norm_x, norm_y, norm_z = path_norms[i]
        
        row_indices = []
        for j in range(K + 1):
            phi = (j / float(K)) * math.pi  # 0 (inner rim edge) to PI (outer rim bottom)
            cos_phi = math.cos(phi)
            sin_phi = math.sin(phi)
            
            # Position
            vx = center_x + norm_x * (-cos_phi * tube_r)
            vy = center_y + (sin_phi * tube_h)
            vz = center_z + norm_z * (-cos_phi * tube_r)
            
            # Normal
            nx = norm_x * (-cos_phi)
            ny = sin_phi
            nz = norm_z * (-cos_phi)
            nmag = math.sqrt(nx*nx + ny*ny + nz*nz)
            if nmag > 0.0001:
                nx /= nmag
                ny /= nmag
                nz /= nmag
                
            u = i / float(M)
            v = j / float(K)
            
            idx = add_vertex((vx, vy, vz), (nx, ny, nz), (u, v))
            row_indices.append(idx)
        tube_vert_indices.append(row_indices)
        
    for i in range(M):
        next_i = (i + 1) % M
        for j in range(K):
            v00 = tube_vert_indices[i][j]
            v01 = tube_vert_indices[i][j + 1]
            v10 = tube_vert_indices[next_i][j]
            v11 = tube_vert_indices[next_i][j + 1]
            
            groups["SlotRim"]["faces"].append((v00, v00, v00, v01, v01, v01, v10, v10, v10))
            groups["SlotRim"]["faces"].append((v01, v01, v01, v11, v11, v11, v10, v10, v10))
            
    # -------------------------------------------------------------
    # 2. GENERATE FLAT DARK CAVITY PIT
    # -------------------------------------------------------------
    pit_y = 0.012
    pit_center_idx = add_vertex((0.0, pit_y, 0.0), (0.0, 1.0, 0.0), (0.5, 0.5))
    
    pit_perim = []
    # Top line
    pit_perim.append((half_l, pit_y, r_inner))
    pit_perim.append((-half_l, pit_y, r_inner))
    
    # Left semicircle
    for i in range(arc_res + 1):
        ang = math.pi * 0.5 + (i / arc_res) * math.pi
        px = -half_l + r_inner * math.cos(ang)
        pz = r_inner * math.sin(ang)
        pit_perim.append((px, pit_y, pz))
        
    # Bottom line
    pit_perim.append((-half_l, pit_y, -r_inner))
    pit_perim.append((half_l, pit_y, -r_inner))
    
    # Right semicircle
    for i in range(arc_res + 1):
        ang = -math.pi * 0.5 + (i / arc_res) * math.pi
        px = half_l + r_inner * math.cos(ang)
        pz = r_inner * math.sin(ang)
        pit_perim.append((px, pit_y, pz))
        
    pit_perim_indices = []
    for p in pit_perim:
        u = (p[0] + outer_w * 0.5) / outer_w
        v = (p[2] + outer_h * 0.5) / outer_h
        idx = add_vertex(p, (0.0, 1.0, 0.0), (u, v))
        pit_perim_indices.append(idx)
        
    for i in range(len(pit_perim_indices)):
        next_i = (i + 1) % len(pit_perim_indices)
        v1 = pit_center_idx
        v2 = pit_perim_indices[i]
        v3 = pit_perim_indices[next_i]
        groups["SlotPit"]["faces"].append((v1, v1, v1, v2, v2, v2, v3, v3, v3))
        
    # -------------------------------------------------------------
    # 3. GENERATE INTEGRATED RIGHT-SIDE BADGE
    # -------------------------------------------------------------
    badge_y = 0.024
    badge_left_x = half_l - 0.12
    corner_r = 0.06
    center_badge_x = (badge_left_x + half_l + r_inner) * 0.5
    
    badge_center_idx = add_vertex((center_badge_x, badge_y, 0.0), (0.0, 1.0, 0.0), (0.5, 0.5))
    
    badge_perim = []
    # 1. Top line to top-left corner
    badge_perim.append((half_l, badge_y, r_inner))
    badge_perim.append((badge_left_x + corner_r, badge_y, r_inner))
    
    # 2. Top-left rounded corner (pi/2 to pi)
    for i in range(7):
        ang = math.pi * 0.5 + (i / 6.0) * math.pi * 0.5
        bx = badge_left_x + corner_r + corner_r * math.cos(ang)
        bz = r_inner - corner_r + corner_r * math.sin(ang)
        badge_perim.append((bx, badge_y, bz))
        
    # 3. Left vertical line
    badge_perim.append((badge_left_x, badge_y, -r_inner + corner_r))
    
    # 4. Bottom-left rounded corner (pi to 3pi/2)
    for i in range(7):
        ang = math.pi + (i / 6.0) * math.pi * 0.5
        bx = badge_left_x + corner_r + corner_r * math.cos(ang)
        bz = -r_inner + corner_r + corner_r * math.sin(ang)
        badge_perim.append((bx, badge_y, bz))
        
    # 5. Bottom line to right arc
    badge_perim.append((half_l, badge_y, -r_inner))
    
    # 6. Right semicircle (-pi/2 to +pi/2)
    for i in range(arc_res + 1):
        ang = -math.pi * 0.5 + (i / arc_res) * math.pi
        bx = half_l + r_inner * math.cos(ang)
        bz = r_inner * math.sin(ang)
        badge_perim.append((bx, badge_y, bz))
        
    badge_perim_indices = []
    for p in badge_perim:
        u = (p[0] - badge_left_x) / (half_l + r_inner - badge_left_x)
        v = (p[2] + r_inner) / (2 * r_inner)
        idx = add_vertex(p, (0.0, 1.0, 0.0), (u, v))
        badge_perim_indices.append(idx)
        
    for i in range(len(badge_perim_indices) - 1):
        v1 = badge_center_idx
        v2 = badge_perim_indices[i]
        v3 = badge_perim_indices[i + 1]
        groups["SlotBadge"]["faces"].append((v1, v1, v1, v2, v2, v2, v3, v3, v3))
    groups["SlotBadge"]["faces"].append((badge_center_idx, badge_center_idx, badge_center_idx, badge_perim_indices[-1], badge_perim_indices[-1], badge_perim_indices[-1], badge_perim_indices[0], badge_perim_indices[0], badge_perim_indices[0]))
    
    # -------------------------------------------------------------
    # 4. GENERATE 3D EXTRUDED NUMBER "1"
    # -------------------------------------------------------------
    y_bot = 0.024
    y_top = 0.038
    cx = center_badge_x
    
    num_poly_2d = [
        (cx - 0.065, -0.11),   # 0: bottom-left foot
        (cx + 0.065, -0.11),   # 1: bottom-right foot
        (cx + 0.065, -0.075),  # 2: bottom-right foot top
        (cx + 0.025, -0.075),  # 3: right stem base
        (cx + 0.025, 0.125),   # 4: top-right stem
        (cx - 0.020, 0.125),   # 5: top-left stem
        (cx - 0.070, 0.080),   # 6: beak tip
        (cx - 0.070, 0.045),   # 7: beak bottom
        (cx - 0.020, 0.075),   # 8: beak-stem junction
        (cx - 0.020, -0.075),  # 9: left stem base
        (cx - 0.065, -0.075)   # 10: bottom-left foot top
    ]
    
    top_v_indices = []
    for x, z in num_poly_2d:
        idx = add_vertex((x, y_top, z), (0.0, 1.0, 0.0), (x, z))
        top_v_indices.append(idx)
        
    top_tris = [
        (0, 1, 2), (0, 2, 10), (10, 2, 3), (10, 3, 9), # Base foot
        (9, 3, 4), (9, 4, 5), (9, 5, 8),                # Main stem
        (8, 5, 6), (8, 6, 7)                            # Beak flag
    ]
    for i1, i2, i3 in top_tris:
        v1 = top_v_indices[i1]
        v2 = top_v_indices[i2]
        v3 = top_v_indices[i3]
        groups["NumberOne"]["faces"].append((v1, v1, v1, v2, v2, v2, v3, v3, v3))
        
    num_pts = len(num_poly_2d)
    for i in range(num_pts):
        next_i = (i + 1) % num_pts
        x1, z1 = num_poly_2d[i]
        x2, z2 = num_poly_2d[next_i]
        
        dx = x2 - x1
        dz = z2 - z1
        side_nx = dz
        side_nz = -dx
        smag = math.sqrt(side_nx * side_nx + side_nz * side_nz)
        if smag > 0.0001:
            side_nx /= smag
            side_nz /= smag
            
        v_t1 = add_vertex((x1, y_top, z1), (side_nx, 0.0, side_nz), (0.0, 1.0))
        v_t2 = add_vertex((x2, y_top, z2), (side_nx, 0.0, side_nz), (1.0, 1.0))
        v_b2 = add_vertex((x2, y_bot, z2), (side_nx, 0.0, side_nz), (1.0, 0.0))
        v_b1 = add_vertex((x1, y_bot, z1), (side_nx, 0.0, side_nz), (0.0, 0.0))
        
        groups["NumberOne"]["faces"].append((v_t1, v_t1, v_t1, v_t2, v_t2, v_t2, v_b2, v_b2, v_b2))
        groups["NumberOne"]["faces"].append((v_t1, v_t1, v_t1, v_b2, v_b2, v_b2, v_b1, v_b1, v_b1))
        
    # WRITE .OBJ FILE
    with open(output_obj_path, "w") as f:
        f.write("# Hole Stadium Capsule 3D Model\n")
        f.write(f"mtllib {output_mtl_path.split('/')[-1]}\n\n")
        
        for v in verts:
            f.write(f"v {v[0]:.6f} {v[1]:.6f} {v[2]:.6f}\n")
        f.write("\n")
        
        for vn in normals:
            f.write(f"vn {vn[0]:.6f} {vn[1]:.6f} {vn[2]:.6f}\n")
        f.write("\n")
        
        for vt in uvs:
            f.write(f"vt {vt[0]:.6f} {vt[1]:.6f}\n")
        f.write("\n")
        
        for gname, gdata in groups.items():
            f.write(f"g {gname}\n")
            f.write(f"usemtl {gdata['mtl']}\n")
            for face in gdata["faces"]:
                f.write(f"f {face[0]}/{face[1]}/{face[2]} {face[3]}/{face[4]}/{face[5]} {face[6]}/{face[7]}/{face[8]}\n")
            f.write("\n")
            
    # WRITE .MTL FILE
    with open(output_mtl_path, "w") as f:
        f.write("# Material definitions for Hole_Stadium\n\n")
        
        # Mat_Rim_Red
        f.write("newmtl Mat_Rim_Red\n")
        f.write("Ka 0.95 0.24 0.26\n")
        f.write("Kd 0.95 0.24 0.26\n")
        f.write("Ks 0.94 0.94 0.94\n")
        f.write("Ns 220.0\n")
        f.write("d 1.0\n")
        f.write("illum 2\n\n")
        
        # Mat_Pit_Dark
        f.write("newmtl Mat_Pit_Dark\n")
        f.write("Ka 0.0 0.0 0.0\n")
        f.write("Kd 0.0 0.0 0.0\n")
        f.write("Ks 0.12 0.12 0.12\n")
        f.write("Ns 15.0\n")
        f.write("d 1.0\n")
        f.write("illum 2\n\n")
        
        # Mat_Badge_Red
        f.write("newmtl Mat_Badge_Red\n")
        f.write("Ka 0.95 0.24 0.26\n")
        f.write("Kd 0.95 0.24 0.26\n")
        f.write("Ks 0.90 0.90 0.90\n")
        f.write("Ns 180.0\n")
        f.write("d 1.0\n")
        f.write("illum 2\n\n")
        
        # Mat_Number_White
        f.write("newmtl Mat_Number_White\n")
        f.write("Ka 1.0 1.0 1.0\n")
        f.write("Kd 1.0 1.0 1.0\n")
        f.write("Ks 0.6 0.6 0.6\n")
        f.write("Ns 120.0\n")
        f.write("d 1.0\n")
        f.write("illum 2\n\n")

if __name__ == "__main__":
    generate_hole_obj("Assets/Models/Hole_Stadium.obj", "Assets/Models/Hole_Stadium.mtl")
    generate_hole_obj("Assets/Prefabs/HOLEE.obj", "Assets/Prefabs/HOLEE.mtl")
    generate_hole_obj("Assets/Prefabs/Hole_Red_New.obj", "Assets/Prefabs/Hole_Red_New.mtl")
    print("Successfully generated Hole 3D models!")
