from PIL import Image
import numpy as np

img = Image.open('Assets/UI/Sprites/LevelCompleted/LevelCompleted_Title.png').convert("RGBA")
arr = np.array(img)
H, W, _ = arr.shape

# Clear any non-transparent pixel in the bottom 25 rows that isn't gold
for y in range(H - 30, H):
    for x in range(W):
        r, g, b, a = arr[y, x]
        if a > 0:
            # Gold is R > 150, G > 100, B < 80
            if not (r > 150 and g > 100 and b < 80):
                arr[y, x, 3] = 0

alpha = arr[:, :, 3]
non_empty_columns = np.where(alpha.max(axis=0) > 0)[0]
non_empty_rows = np.where(alpha.max(axis=1) > 0)[0]
crop_bbox = (non_empty_columns[0], non_empty_rows[0], non_empty_columns[-1] + 1, non_empty_rows[-1] + 1)
clean_img = Image.fromarray(arr).crop(crop_bbox)

padded = Image.new("RGBA", (clean_img.width + 4, clean_img.height + 4), (0, 0, 0, 0))
padded.paste(clean_img, (2, 2))
padded.save('Assets/UI/Sprites/LevelCompleted/LevelCompleted_Title.png')
print("Perfect title saved! Size:", padded.size)
