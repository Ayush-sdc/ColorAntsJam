from PIL import Image
import numpy as np

img = Image.open('Assets/UI/Sprites/LevelCompleted/LevelCompleted_Title.png').convert("RGBA")
arr = np.array(img)

# Clear any stray pixels outside the main banner contour at the bottom left and bottom right
# Bottom row is at H-1. Let's clear any isolated blue specks below the golden bottom border.
H, W, _ = arr.shape
for y in range(H - 30, H):
    for x in range(W):
        r, g, b, a = arr[y, x]
        if a > 0:
            # If it's a stray dark blue pixel below the golden banner (not part of the gold trim)
            if b > r + 30 and g < 100 and r < 80 and (x < 150 or x > W - 150):
                arr[y, x, 3] = 0

# Also check for isolated small components
clean_img = Image.fromarray(arr)
clean_img.save('Assets/UI/Sprites/LevelCompleted/LevelCompleted_Title.png')
print("Cleaned specks and saved!")
