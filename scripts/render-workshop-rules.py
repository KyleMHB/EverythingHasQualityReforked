"""Compose the rules panel with original RimWorld sprites and plotted example curves.

Requires Pillow and zstandard. Reads the local game archive without changing it.
All lettering is rendered from the local RimWordFont file.
"""
import argparse
import runpy
import io
import struct
import zipfile
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
try:
    import zstandard
except ModuleNotFoundError:
    # Reuse the sprite extraction dependency already available in this workspace.
    sys.path.insert(0, str(ROOT / 'artifacts/steam-page-mockup/tools'))
    import zstandard
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--archive', type=Path, default=Path(r'E:\Steam\steamapps\common\RimWorld\Assets.zip'))
parser.add_argument('--font', type=Path, default=ROOT / 'artifacts/steam-page-mockup/RimWordFont.ttf')
args = parser.parse_args()
values = runpy.run_path(str(ROOT/'scripts/workshop-values.py'))['load_values'](ROOT,args.archive.parent)
target = ROOT / 'docs/images/workshop/quality-rules.png'
image = Image.new('RGB', (1672, 941), '#10232f')
draw = ImageDraw.Draw(image)
navy, cream, gold = '#10232f', '#f5eedc', '#edc76c'
tiers = ['#c98079', '#bba485', '#ece5cc', '#86b989', '#7dc5d5', '#c3a2db', '#efc861']
draw.rectangle((0, 151, 1671, 826), fill=navy)

def font(size):
    return ImageFont.truetype(str(args.font), size)

def label(x, y, text, size, color=cream, anchor='lt', width=None):
    # The font's capital O is a planet emblem; its lowercase o is the plain letter.
    text = text.replace('O', 'o')
    while width and draw.textlength(text, font=font(size)) > width:
        size -= 1
    draw.text((x, y), text, font=font(size), fill=color, anchor=anchor)

label(836, 35, 'Broad rules. Precise exceptions.', 85, gold, anchor='mt', width=1560)
for tier, color in enumerate(tiers):
    draw.rectangle((round(tier*1672/7), 828, round((tier+1)*1672/7), 844), fill=color)
label(836, 858, 'Separate rules for assignment and effects.', 40, anchor='mt', width=1560)
label(836, 904, 'Prices and vanilla removal have their own rules.', 40, anchor='mt', width=1560)

def extract(archive, stream, path):
    info = archive.getinfo('Resources/Textures/' + path)
    stream.seek(info.header_offset)
    header = stream.read(30)
    name_size, extra_size = struct.unpack_from('<HH', header, 26)
    stream.seek(name_size + extra_size, 1)
    compressed = stream.read(info.compress_size)
    if info.compress_type == 93:
        raw = zstandard.ZstdDecompressor().decompress(compressed, max_output_size=info.file_size)
    else:
        raw = archive.read(info)
    return Image.open(io.BytesIO(raw)).convert('RGBA')

with zipfile.ZipFile(args.archive) as archive, args.archive.open('rb') as stream:
    atlas = extract(archive, stream, 'Things/Building/Linked/Wall/Wall_Atlas_Bricks.psd.dds')
    wall = atlas.crop((0, 240, 80, 320)).crop((11, 10, 69, 69))
    alpha = wall.getchannel('A')
    wall = ImageChops.multiply(wall, Image.new('RGBA', wall.size, (105, 95, 97, 255)))
    wall.putalpha(alpha)
    bed = extract(archive, stream, 'Things/Building/Furniture/Bed/Bed_south.psd.dds')
    mask = extract(archive, stream, 'Things/Building/Furniture/Bed/Bed_southm.psd.dds')
    wood = ImageChops.multiply(bed, Image.new('RGBA', bed.size, (133, 97, 67, 255)))
    sheets = ImageChops.multiply(bed, Image.new('RGBA', bed.size, (161, 213, 180, 255)))
    bed = Image.composite(wood, bed, mask.getchannel('R'))
    bed = Image.composite(sheets, bed, mask.getchannel('G'))
    sprites = {
        'wall': wall,
        'bed': bed,
        'rifle': extract(archive, stream, 'Things/Item/Equipment/WeaponRanged/AssaultRifle.png.dds'),
        'shelf': extract(archive, stream, 'Things/Building/Furniture/Shelf_south.psd.dds'),
        'door': extract(archive, stream, 'Things/Building/Door/DoorSimple_MenuIcon.psd.dds'),
    }

def sprite(name, box):
    art = sprites[name]
    art = art.crop(art.getbbox())
    left, top, right, bottom = box
    scale = min((right-left)/art.width, (bottom-top)/art.height)
    art = art.resize((round(art.width*scale), round(art.height*scale)), Image.Resampling.NEAREST)
    image.paste(art, (round((left+right-art.width)/2), round((top+bottom-art.height)/2)), art)

label(836, 175, 'Global defaults. Group, category and item overrides.', 43, gold, anchor='mt', width=1550)
def rule_row(y, indent, title, enabled, icon):
    x, right = 75+indent, 1597
    draw.rounded_rectangle((x,y,right,y+132),radius=7,fill='#243a46',outline='#49606b',width=2)
    label(x+28,y+38,title,48,width=right-x-650)
    label(1125,y+44,'Effects ON' if enabled else 'Effects OFF',43,'#8abe95' if enabled else '#c4cbd0',anchor='mt',width=400)
    sprite(icon,(right-150,y+11,right-28,y+120))

rule_row(238,0,'Group: Buildings',True,'wall')
rule_row(424,100,'Category: Furniture',False,'shelf')
rule_row(610,200,'Item: Bed',True,'bed')
# Draw arrow edges at 4x resolution before compositing them into the panel.
arrow_layer=Image.new('RGBA',(1672*4,941*4))
ad=ImageDraw.Draw(arrow_layer)
for x,top,next_x,center_y in [(125,370,175,490),(225,556,275,676)]:
    ad.line([(x*4,top*4),(x*4,center_y*4),((next_x-18)*4,center_y*4)],fill=gold,width=36,joint='curve')
    ad.polygon([((next_x-21)*4,(center_y-17)*4),((next_x-21)*4,(center_y+17)*4),(next_x*4,center_y*4)],fill=gold)
arrow_layer=arrow_layer.resize(image.size,Image.Resampling.LANCZOS)
image.paste(arrow_layer,(0,0),arrow_layer)
label(836, 778, 'Bed ON overrides Furniture OFF.', 36, anchor='mt', width=1550)
image.save(target)
image.resize((636,358),Image.Resampling.LANCZOS).save(ROOT/'artifacts/quality-rules-steam-width.png')

# Strength settings get their own full-width panel rather than sharing the rules graphic.
image = Image.new('RGB',(1672,941),navy)
draw = ImageDraw.Draw(image)
label(836,35,'Tune every quality level',88,gold,anchor='mt',width=1560)
for tier,color in enumerate(tiers):
    draw.rectangle((round(tier*1672/7),151,round((tier+1)*1672/7),166),fill=color)
    draw.rectangle((round(tier*1672/7),828,round((tier+1)*1672/7),844),fill=color)
curves = [(name,values[name.lower()]) for name in ['Mild','Medium','Progression']]
for index, (name, curve_values) in enumerate(curves):
    x, y, w, h = 65 + index*532, 272, 478, 480
    label(x+w/2, 203, name, 52, anchor='mt')
    draw.rounded_rectangle((x, y, x+w, y+h), radius=8, fill='#152c39', outline='#4c6370', width=2)
    left, right, top, bottom = x+35, x+w-30, y+30, y+h-48
    def py(value):
        return bottom - value / 2.6 * (bottom-top)
    for value in (.5, 1, 1.5, 2, 2.5):
        yy = py(value)
        draw.line((left, yy, right, yy), fill='#647b84' if value == 1 else '#2d4551', width=2 if value == 1 else 1)
    points = [(left+t*(right-left)/6, py(value)) for t, value in enumerate(curve_values)]
    for xx, yy in points:
        draw.line((xx, top, xx, bottom), fill='#2d4551', width=1)
    # Supersample the curve and nodes so diagonal edges stay smooth at Steam width.
    scale=3
    layer=Image.new('RGBA',(w*scale,h*scale))
    ld=ImageDraw.Draw(layer)
    # Cubic Hermite interpolation with flat tangents at turning points avoids overshoot.
    slopes=[(points[j+1][1]-points[j][1])/(points[j+1][0]-points[j][0]) for j in range(6)]
    tangents=[slopes[0]]+[0 if slopes[j-1]*slopes[j]<=0 else 2*slopes[j-1]*slopes[j]/(slopes[j-1]+slopes[j]) for j in range(1,6)]+[slopes[-1]]
    smooth=[]
    for j in range(6):
        ax,ay=points[j]; bx,by=points[j+1]; dx=bx-ax
        for step in range(40):
            t=step/40
            yy=(2*t**3-3*t**2+1)*ay+(t**3-2*t**2+t)*dx*tangents[j]+(-2*t**3+3*t**2)*by+(t**3-t**2)*dx*tangents[j+1]
            smooth.append(((ax+t*dx-x)*scale,(yy-y)*scale))
    smooth.append(((points[-1][0]-x)*scale,(points[-1][1]-y)*scale))
    ld.line(smooth,fill=cream,width=7*scale,joint='curve')
    for (xx,yy),color in zip(points,tiers):
        xx,yy=(xx-x)*scale,(yy-y)*scale
        ld.ellipse((xx-14*scale,yy-14*scale,xx+14*scale,yy+14*scale),fill=color,outline=navy,width=3*scale)
    image.paste(layer.resize((w,h),Image.Resampling.LANCZOS),(x,y),layer.resize((w,h),Image.Resampling.LANCZOS))
    label(x+w/2, y+h-39, 'Awful to Legendary', 34, '#bdcbd0', anchor='mt', width=w-20)
label(836, 775, 'Benefit factors. Normal quality stays at 1x.', 41, anchor='mt', width=1550)
label(836,872,'Progression uses Mild factors. Edit any tier.',44,anchor='mt',width=1550)
image.save(ROOT/'docs/images/workshop/quality-strength.png')
print('Rendered separate rules and strength panels with RimWordFont and native sprites.')
