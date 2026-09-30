import os
from PIL import Image, ImageDraw

os.makedirs('Assets/UI/Sprites/Settings', exist_ok=True)

# 1. Settings Panel Background (rounded rectangle, 9-sliceable, 256x256, corner radius 48)
panel_size = 256
panel_img = Image.new("RGBA", (panel_size, panel_size), (0, 0, 0, 0))
draw = ImageDraw.Draw(panel_img)
# Purple color sampled from reference: (138, 56, 243)
draw.rounded_rectangle([0, 0, panel_size - 1, panel_size - 1], radius=48, fill=(138, 56, 243, 255))
panel_img.save('Assets/UI/Sprites/Settings/Settings_Panel_Bg.png')

# 2. Switch Background ON (lime green rounded pill, 120x60, radius 30)
sw_on = Image.new("RGBA", (120, 60), (0, 0, 0, 0))
draw_on = ImageDraw.Draw(sw_on)
draw_on.rounded_rectangle([0, 0, 119, 59], radius=30, fill=(57, 232, 29, 255))
sw_on.save('Assets/UI/Sprites/Settings/Switch_Bg_On.png')

# 3. Switch Background OFF (dark purple/grey rounded pill, 120x60, radius 30)
sw_off = Image.new("RGBA", (120, 60), (0, 0, 0, 0))
draw_off = ImageDraw.Draw(sw_off)
draw_off.rounded_rectangle([0, 0, 119, 59], radius=30, fill=(75, 43, 133, 255))
sw_off.save('Assets/UI/Sprites/Settings/Switch_Bg_Off.png')

# 4. Switch Knob ON (darker green circle, 52x52)
knob_on = Image.new("RGBA", (52, 52), (0, 0, 0, 0))
draw_ko = ImageDraw.Draw(knob_on)
draw_ko.ellipse([0, 0, 51, 51], fill=(27, 176, 8, 255))
knob_on.save('Assets/UI/Sprites/Settings/Switch_Knob_On.png')

# 5. Switch Knob OFF (light grey circle, 52x52)
knob_off = Image.new("RGBA", (52, 52), (0, 0, 0, 0))
draw_kf = ImageDraw.Draw(knob_off)
draw_kf.ellipse([0, 0, 51, 51], fill=(185, 185, 195, 255))
knob_off.save('Assets/UI/Sprites/Settings/Switch_Knob_Off.png')

# 6. Privacy Button Background (darker purple rounded pill, 240x60, radius 30)
priv_img = Image.new("RGBA", (240, 60), (0, 0, 0, 0))
draw_priv = ImageDraw.Draw(priv_img)
draw_priv.rounded_rectangle([0, 0, 239, 59], radius=30, fill=(93, 39, 176, 255))
priv_img.save('Assets/UI/Sprites/Settings/Privacy_Button_Bg.png')

print("Created all Settings UI sprites successfully!")
