from PIL import Image
import numpy as np

# Open reference image
ref = Image.open(r'C:\Users\ayush\Desktop\AyushGameSS\UI elements\Level_completed.png').convert("RGBA")
w, h = ref.size

# We found earlier the title is centered horizontally around w // 2 = 1470
# Vertically, let's find the exact bounds of the yellow/orange backing of "LEVEL COMPLETED!"
# In scratch_ref_title.png, crop was (w // 2 - 600, int(h * 0.15), w // 2 + 600, int(h * 0.45))
crop_box = (w // 2 - 650, int(h * 0.14), w // 2 + 650, int(h * 0.44))
cropped = ref.crop(crop_box)

# Now, let's key out the background.
# The background behind the emblem is dark grey translucent backdrop over the board.
# The emblem has a bright golden/yellow outer border with blue inner shadows.
# Let's inspect background color at the corners:
arr = np.array(cropped)
corner_color = arr[0, 0][:3]
print("Corner color:", corner_color)

# We can perform a flood fill or color difference mask from corners/edges:
# Any pixel from the outside that is dark grey / brown (background) is made transparent (alpha = 0).
from collections import deque

H, W, _ = arr.shape
mask = np.zeros((H, W), dtype=bool)

# Breadth-first search from all 4 borders
q = deque()
for x in range(W):
    q.append((0, x))
    q.append((H - 1, x))
for y in range(H):
    q.append((y, 0))
    q.append((y, W - 1))

# The emblem's outer boundary is either the golden/yellow rim (R > 180, G > 140) or dark blue outline (R < 30, B > 100)
# The background is dark neutral grey: (R, G, B) are all roughly equal and < 70, with R ~ G ~ B.
while q:
    y, x = q.popleft()
    if mask[y, x]:
        continue
    mask[y, x] = True
    
    for dy, dx in [(-1,0), (1,0), (0,-1), (0,1)]:
        ny, nx = y + dy, x + dx
        if 0 <= ny < H and 0 <= nx < W and not mask[ny, nx]:
            r, g, b, a = arr[ny, nx]
            # Check if this pixel is background
            # Background is dark: max(r,g,b) < 95, or neutral grey: abs(r-g) < 15 and abs(r-b) < 15 and max(r,g,b) < 120
            # Emblem boundary has strong yellow (r > 180, g > 130) or rich blue (b > 120, b - r > 40)
            is_gold = (r > 160 and g > 120 and b < 80)
            is_blue = (b > 100 and b - r > 30)
            is_white = (r > 200 and g > 200 and b > 200)
            if not (is_gold or is_blue or is_white):
                q.append((ny, nx))

# Any pixel in mask is transparent
arr[mask, 3] = 0

# Crop bounding box of non-transparent pixels
alpha = arr[:, :, 3]
non_empty_columns = np.where(alpha.max(axis=0) > 0)[0]
non_empty_rows = np.where(alpha.max(axis=1) > 0)[0]
crop_bbox = (non_empty_columns[0], non_empty_rows[0], non_empty_columns[-1] + 1, non_empty_rows[-1] + 1)
clean_img = Image.fromarray(arr).crop(crop_bbox)

# Add a slight padding of 4 pixels
padded = Image.new("RGBA", (clean_img.width + 8, clean_img.height + 8), (0, 0, 0, 0))
padded.paste(clean_img, (4, 4))

padded.save('Assets/UI/Sprites/LevelCompleted/LevelCompleted_Title.png')
print("Successfully saved full LEVEL COMPLETED! title to Assets/UI/Sprites/LevelCompleted/LevelCompleted_Title.png")
print("Dimensions:", padded.size)
