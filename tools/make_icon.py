"""Draws the game icon (game/icon.png 512 px, game/icon.ico 16-256 px): a fish leaping over the water in front of
a low sun, in the title palette. Run: python tools/make_icon.py"""
import math, os
from PIL import Image, ImageDraw

S = 1024  # drawn large, scaled down for clean edges
TEAL, DEEP, WATER, LINE = (19, 52, 59), (10, 30, 36), (27, 84, 92), (120, 190, 190)
CREAM, GOLD, INK = (244, 236, 214), (240, 190, 90), (19, 40, 44)

img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
mask = Image.new("L", (S, S), 0)
ImageDraw.Draw(mask).rounded_rectangle([0, 0, S - 1, S - 1], radius=210, fill=255)

bg = Image.new("RGBA", (S, S), TEAL)
d = ImageDraw.Draw(bg)
for y in range(S):  # sky: lighter toward the horizon
    t = y / S
    c = tuple(int(TEAL[i] + (54, 98, 100)[i] * 0.35 * t) for i in range(3))
    d.line([(0, y), (S, y)], fill=c)
horizon = int(S * 0.64)
d.ellipse([S * 0.52, horizon - 250, S * 0.52 + 380, horizon + 130], fill=GOLD)  # sun, half under the water
# Water with a wave on top.
pts = [(x, horizon + 18 * math.sin(x / S * math.tau * 2.2)) for x in range(0, S + 8, 8)]
d.polygon(pts + [(S, S), (0, S)], fill=WATER)
d.line(pts, fill=LINE, width=14)
for k, y in enumerate([horizon + 120, horizon + 220]):
    d.line([(S * (0.18 + 0.1 * k), y), (S * (0.5 + 0.08 * k), y)], fill=DEEP, width=12)

# The fish: a leaping body along an arc, forked tail, eye.
fish = Image.new("RGBA", (S, S), (0, 0, 0, 0))
f = ImageDraw.Draw(fish)
cx, cy, L, H = S * 0.46, S * 0.5, 600, 215
body = []
for i in range(0, 61):
    u = i / 60
    x = -L / 2 + L * u
    half = H / 2 * math.sin(math.pi * min(1.0, u * 1.05)) ** 0.8 * (1 - 0.35 * u)
    body.append((x, -half))
for i in range(60, -1, -1):
    u = i / 60
    x = -L / 2 + L * u
    half = H / 2 * math.sin(math.pi * min(1.0, u * 1.05)) ** 0.8 * (1 - 0.35 * u) * 0.8
    body.append((x, half))
tail_x = L / 2 - 20
tail = [(tail_x, -8), (tail_x + 150, -120), (tail_x + 105, 0), (tail_x + 150, 110), (tail_x, 10)]
fin = [(-40, -90), (60, -150), (120, -70)]
ang = math.radians(-28)

def place(p):
    x, y = p
    x = -x  # head on the left, leaping up and to the left
    return (cx + x * math.cos(ang) - y * math.sin(ang), cy + x * math.sin(ang) + y * math.cos(ang))

f.polygon([place(p) for p in fin], fill=CREAM)
f.polygon([place(p) for p in body], fill=CREAM)
f.polygon([place(p) for p in tail], fill=CREAM)
ex, ey = place((-L / 2 + 95, -18))
f.ellipse([ex - 24, ey - 24, ex + 24, ey + 24], fill=INK)
mx, my = place((-L / 2 + 10, 12))
f.line([place((-L / 2 + 8, 14)), place((-L / 2 + 60, 22))], fill=INK, width=10)  # mouth

bg.alpha_composite(fish)
img.paste(bg, (0, 0), mask)

root = os.path.join(os.path.dirname(__file__), "..", "game")
big = img.resize((512, 512), Image.LANCZOS)
big.save(os.path.join(root, "icon.png"))
img.resize((256, 256), Image.LANCZOS).save(os.path.join(root, "icon.ico"), sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)])
print("wrote game/icon.png and game/icon.ico")
