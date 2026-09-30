import os
import numpy as np
from PIL import Image

brain_dir = r"C:\Users\ayush\.gemini\antigravity-ide\brain\183c4cda-2625-4685-acd6-50a0b4f7f593"
output_dir = r"c:\Users\ayush\Desktop\Unity Projects\Slither Ants Jam\Assets\UI\Sprites\Features"

images = {
    "Illustration_LinkedAnts.png": "illustration_linked_ants_1790804846419.jpg",
    "Illustration_StackedAnts.png": "illustration_stacked_ants_1790804781538.jpg",
    "Illustration_Pipe.png": "illustration_pipe_1790804800676.jpg",
    "Illustration_LockedAnts.png": "illustration_locked_ants_1790804823366.jpg"
}

def make_transparent(input_path, output_path, icon_path=None):
    img = Image.open(input_path).convert("RGBA")
    data = np.array(img, dtype=np.float32)
    
    # White background removal with smooth transition
    r, g, b, a = data[:,:,0], data[:,:,1], data[:,:,2], data[:,:,3]
    
    # Distance from pure white (255, 255, 255)
    # Brightness metric
    brightness = (r + g + b) / 3.0
    color_diff = np.max(np.abs(data[:,:,:3] - brightness[:,:,None]), axis=2)
    
    # Near pure white (high brightness, low color diff) -> transparent
    # Smooth alpha feathering
    is_white_bg = (brightness > 240) & (color_diff < 12)
    
    # Flood fill / corner-connected flood fill or thresholding with edge feathering
    # For studio white backgrounds:
    alpha = np.ones_like(brightness) * 255.0
    
    # Thresholds: > 250 is fully transparent, between 230 and 250 feathers
    bg_factor = np.clip((brightness - 235.0) / 18.0, 0.0, 1.0)
    # only apply if color variation is small (i.e. truly white/grey background, not colored ant parts)
    is_neutral = np.clip((15.0 - color_diff) / 15.0, 0.0, 1.0)
    transparency = bg_factor * is_neutral
    
    alpha = (1.0 - transparency) * 255.0
    
    # For shadows/feet touching ground: fade out very light grey floor shadows
    floor_shadow = (brightness > 220) & (color_diff < 8)
    alpha[floor_shadow] = np.minimum(alpha[floor_shadow], (255.0 - brightness[floor_shadow]) * 2.0)
    
    # Clean edges
    alpha = np.clip(alpha, 0, 255).astype(np.uint8)
    
    # Reassemble RGBA
    out_arr = np.dstack((data[:,:,0].astype(np.uint8), data[:,:,1].astype(np.uint8), data[:,:,2].astype(np.uint8), alpha))
    out_img = Image.fromarray(out_arr, "RGBA")
    
    # Autocrop to visible content with small padding
    bbox = out_img.getbbox()
    if bbox:
        # crop with 20px padding
        pad = 20
        w, h = out_img.size
        crop_box = (max(0, bbox[0]-pad), max(0, bbox[1]-pad), min(w, bbox[2]+pad), min(h, bbox[3]+pad))
        cropped = out_img.crop(crop_box)
        # Pad back to square
        max_dim = max(cropped.width, cropped.height)
        square_img = Image.new("RGBA", (max_dim, max_dim), (0, 0, 0, 0))
        square_img.paste(cropped, ((max_dim - cropped.width) // 2, (max_dim - cropped.height) // 2))
        final_img = square_img.resize((1024, 1024), Image.Resampling.LANCZOS)
    else:
        final_img = out_img.resize((1024, 1024), Image.Resampling.LANCZOS)
        
    final_img.save(output_path, "PNG")
    print(f"Saved: {output_path}")
    
    if icon_path:
        icon_img = final_img.resize((256, 256), Image.Resampling.LANCZOS)
        icon_img.save(icon_path, "PNG")
        print(f"Saved Icon: {icon_path}")

for out_name, src_name in images.items():
    in_file = os.path.join(brain_dir, src_name)
    out_file = os.path.join(output_dir, out_name)
    icon_name = out_name.replace("Illustration_", "Icon_")
    icon_file = os.path.join(output_dir, icon_name)
    make_transparent(in_file, out_file, icon_file)
