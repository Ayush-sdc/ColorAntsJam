from PIL import Image
import numpy as np

img = Image.open('Assets/UI/Sprites/LevelCompleted/LevelCompleted_Title.png').convert("RGBA")
arr = np.array(img)
H, W, _ = arr.shape

# The gold banner has a clear bottom edge. Any blue/dark pixels below the gold border (say bottom 35 rows) should be cleared.
for y in range(H - 45, H):
    for x in range(W):
        r, g, b, a = arr[y, x]
        if a > 0:
            # If not gold (gold is r > 160, g > 120)
            if not (r > 160 and g > 110):
                arr[y, x, 3] = 0

# Crop bounding box again
alpha = arr[:, :, 3]
non_empty_columns = np.where(alpha.max(axis=0) > 0)[0]
non_empty_rows = np.where(alpha.max(axis=1) > 0)[0]
crop_bbox = (non_empty_columns[0], non_empty_rows[0], non_empty_columns[-1] + 1, non_empty_rows[-1] + 1)
clean_img = Image.fromarray(arr).crop(crop_bbox)

padded = Image.new("RGBA", (clean_img.width + 8, clean_img.height + 8), (0, 0, 0, 0))
padded.paste(clean_img, (4, 4))
padded.save('Assets/UI/Sprites/LevelCompleted/LevelCompleted_Title.png')
print("Successfully cleaned and trimmed! Size:", padded.size)
