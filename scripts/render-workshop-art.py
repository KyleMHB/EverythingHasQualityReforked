"""Render all public artwork from RimWordFont and original local RimWorld sprites.

No image-generation calls, generated lettering or generated game objects are used.
Pass --archive and --font to override the paths accepted by render-workshop-rules.py.
"""
import json
import runpy
import zipfile
from decimal import Decimal, ROUND_HALF_UP
from functools import lru_cache
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont, ImageChops, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
native = runpy.run_path(str(ROOT / 'scripts/render-workshop-rules.py'))
args, sprites, extract = native['args'], native['sprites'], native['extract']
with zipfile.ZipFile(args.archive) as archive, args.archive.open('rb') as stream:
    sprites['silver'] = extract(archive, stream, 'Things/Item/Resource/Silver/Silver_a.psd.dds')
    sprites['machining'] = extract(archive, stream, 'Things/Building/Production/TableMachining_south.psd.dds')
    sprites['heater'] = extract(archive, stream, 'Things/Building/Misc/TempControl/Heater.psd.dds')
    floor = extract(archive, stream, 'Terrain/Surfaces/WoodFloor.psd.dds')
OUT = ROOT / 'docs/images/workshop'
NAVY, PANEL, CREAM, GOLD = '#10232f', '#233b48', '#f5eedc', '#edc76c'
COLORS = native['tiers']

@lru_cache(maxsize=None)
def font(size):
    return ImageFont.truetype(str(args.font), size)

def text(im, x, y, value, size, fill=CREAM, width=None, anchor='mt'):
    draw = ImageDraw.Draw(im)
    # RimWordFont maps capital O to an emblem and has no percent glyph.
    value = value.replace('O', 'o')
    if value.endswith('%'):
        number = value[:-1]
        def percent_font():
            return ImageFont.truetype('C:/Windows/Fonts/arialbd.ttf', round(size*.9))
        def total_width():
            return draw.textlength(number, font=font(size)) + draw.textlength('%', font=percent_font())
        while width and total_width() > width:
            size -= 1
        left = x-total_width()/2 if anchor=='mt' else x
        draw.text((left,y),number,font=font(size),fill=fill,anchor='lt')
        draw.text((left+draw.textlength(number,font=font(size)),y+size*.03),'%',font=percent_font(),fill=fill,anchor='lt')
        return size
    while width and draw.textlength(value, font=font(size)) > width:
        size -= 1
    draw.text((x, y), value, font=font(size), fill=fill, anchor=anchor)
    return size

def art(im, name, box):
    sprite = sprites[name]
    sprite = sprite.crop(sprite.getchannel('A').point(lambda a: 255 if a > 10 else 0).getbbox())
    x0, y0, x1, y1 = box
    scale = min((x1-x0)/sprite.width, (y1-y0)/sprite.height)
    sprite = sprite.resize((round(sprite.width*scale), round(sprite.height*scale)), Image.Resampling.NEAREST)
    im.paste(sprite, (round((x0+x1-sprite.width)/2), round((y0+y1-sprite.height)/2)), sprite)

def toggle(im, x, y, on, width=145, height=70):
    draw = ImageDraw.Draw(im)
    draw.rounded_rectangle((x,y,x+width,y+height), radius=height//2, fill='#6ba775' if on else '#596168', outline='#091721', width=5)
    radius = height//2-8
    cx = x+width-height//2 if on else x+height//2
    draw.ellipse((cx-radius,y+8,cx+radius,y+height-8), fill=CREAM)

def divider(im, y):
    draw = ImageDraw.Draw(im)
    for i, color in enumerate(COLORS):
        draw.rectangle((round(i*im.width/7),y,round((i+1)*im.width/7),y+15),fill=color)

def panel(title):
    im = Image.new('RGB',(1672,941),NAVY)
    text(im,836,43,title,83,GOLD,width=1550)
    divider(im,151)
    return im

def footer(im, lines):
    divider(im,792)
    for i, line in enumerate(lines):
        text(im,836,825+i*55,line,42,width=1550)

# Assignment choices are examples, never default claims.
im=panel('Choose what has quality')
for i,(name,label,on) in enumerate([('wall','Wall quality',False),('bed','Bed quality',True),('rifle','Rifle quality',False)]):
    cx=285+i*550
    art(im,name,(cx-195,218,cx+195,552))
    text(im,cx,590,label,48,width=490)
    toggle(im,cx-118,674,on)
    text(im,cx+70,688,'ON' if on else 'OFF',40,anchor='lt')
footer(im,['Add quality. Keep it. Or remove vanilla quality.','Vanilla-quality removal is opt-in.'])
im.save(OUT/'quality-choices.png')

im=Image.new('RGB',(1672,1120),NAVY)
text(im,836,43,'Choose what quality does',83,GOLD,width=1550)
divider(im,151)
for i,(name,item,y,height,controls) in enumerate([
    ('wall','Wall',190,130,[('Durability',True,None)]),
    ('machining','Machining table',335,230,[('Work speed',False,None),('Power use',True,None)]),
    ('heater','Heater',580,130,[('Power use',False,None)]),
    ('rifle','Assault rifle',725,230,[('Weapon cooldown',True,None),('Durability',True,None)]),
]):
    if i%2==0: ImageDraw.Draw(im).rectangle((45,y,1627,y+height),fill=PANEL)
    art(im,name,(74,y+14,282,y+height-14))
    text(im,340,y+10,item,40,GOLD,width=970,anchor='lt')
    for j,(label,on,factor) in enumerate(controls):
        cy=y+58+j*79
        text(im,340,cy+5,label,47,width=795,anchor='lt')
        if factor:
            ImageDraw.Draw(im).rounded_rectangle((1170,cy,1310,cy+57),radius=4,fill='#091b26',outline=GOLD,width=2)
            text(im,1240,cy+8,factor,42,width=120)
        text(im,1370,cy+10,'ON' if on else 'OFF',38,width=100)
        toggle(im,1460,cy,on,132,55)
divider(im,989)
text(im,836,1020,'One item can use more than one effect.',43,width=1550)
text(im,836,1073,'Added effects start OFF. You choose what to enable.',40,width=1550)
im.save(OUT/'quality-effects.png')

im=panel('Control quality prices and wealth')
ImageDraw.Draw(im).line((836,190,836,750),fill='#49606b',width=2)
for i,on in enumerate([True,False]):
    cx=418+i*836
    text(im,cx,205,'Price scaling '+('ON' if on else 'OFF'),47,width=740)
    toggle(im,cx-72,285,on)
    art(im,'wall',(cx-100,385,cx+100,575))
    text(im,cx,601,'Legendary',40,GOLD)
    text(im,cx-350,686,'Durability',36,anchor='lt')
    text(im,cx+222,669,'250%',66,width=240)
    text(im,cx-350,774,'Market value',35,anchor='lt')
    text(im,cx+222,757,'5x' if on else '1x',70,GOLD,width=240)
divider(im,865)
text(im,836,897,'Same durability. Your price rules.',43,width=1550)
im.save(OUT/'quality-wealth.png')

# Existing numeric examples retain their published values and use the same real sprites.
tiers=['Awful','Poor','Normal','Good','Excellent','Masterwork','Legendary']
values=native['values']
benefits=values['medium']
market=values['market']
rest=[round(v*100) for v in values['rest']]
for name,file in [('wall','granite-wall.png'),('rifle','assault-rifle.png'),('bed','wooden-bed.png')]:
    height=570 if name=='wall' else 660
    im=Image.new('RGB',(1672,height),NAVY); draw=ImageDraw.Draw(im)
    text(im,26,25,'Quality',36,anchor='lt')
    starts=300; cell=(1672-starts)/7
    for i,tier in enumerate(tiers):
        x=starts+i*cell; cx=x+cell/2
        if i==2: draw.rectangle((x,18,x+cell,height-16),fill=PANEL)
        headings=['Awful','Poor','Normal','Good','Excel.','Master.','Legend.']
        text(im,cx,32,headings[i],32,COLORS[i])
        art(im,name,(x+14,110,x+cell-14,230))
        draw.rectangle((x+14,255,x+cell-14,261),fill=COLORS[i])
        # The fields show durability scaling consistently across all three tables.
        # Rifle cooldown outputs use the separate reciprocal Medium cost curve.
        factor = benefits[i]
        factor_text = f'{factor:.3f}'.rstrip('0').rstrip('.')
        if '.' not in factor_text: factor_text += '.00'
        field=(x+13,297,x+cell-13,356)
        draw.rounded_rectangle(field,radius=4,fill='#091b26',outline=GOLD if i==6 else '#6a818b',width=3)
        text(im,cx,305,factor_text,40,width=cell-45)
        if i==6: draw.line((x+cell-27,312,x+cell-27,347),fill=GOLD,width=2)
        if i==0:
            text(im,26,305,'Scaling',35,width=265,anchor='lt')
            text(im,26,343,'Factor',35,width=265,anchor='lt')
        hp=f'{benefits[i]*100:g}%'
        cooldown=str((Decimal(str(values['cooldown']))/Decimal(str(benefits[i]))).quantize(Decimal('.01'),rounding=ROUND_HALF_UP))+' s'
        rows=([('Max. HP',hp),('Market value',f'{market[i]:g}x')] if name=='wall' else
              [('Cooldown',cooldown),('Max. HP',hp),('Market value',f'{market[i]:g}x')] if name=='rifle' else
              [('Rest effectiveness',f'{rest[i]}%'),('Max. HP',hp),('Market value',f'{market[i]:g}x')])
        for row,(label,value) in enumerate(rows):
            y=410+row*82
            if i==0:
                lines = {'Market value':['Market','value'], 'Rest effectiveness':['Rest','effectiveness']}.get(label,[label])
                for line_index,line in enumerate(lines):
                    text(im,26,y-8+line_index*36 if len(lines)>1 else y,line,35,width=265,anchor='lt')
            text(im,cx,y,value,40,COLORS[i],width=cell-15)
    im.save(OUT/file)

# Use the pack's original local icon, with source credit in the description.
progression_logo = OUT/'progression-logo.png'
if progression_logo.exists():
    badge=Image.new('RGB',(1672,430),NAVY)
    logo=Image.open(progression_logo).convert('RGBA')
    logo.thumbnail((330,330),Image.Resampling.LANCZOS)
    badge.paste(logo,(70+(330-logo.width)//2,(430-logo.height)//2),logo)
    text(badge,1035,68,'Progression preset',70,GOLD,width=1130)
    text(badge,1035,181,'Mild factors. Pack-specific exclusions.',39,width=1130)
    text(badge,1035,255,'Auto Mod Config support',45,width=1130)
    divider(badge,407)
    badge.save(OUT/'progression-support.png')

# Title and preview: original floor and item sprites, with the approved toggle states.
banner=Image.new('RGB',(1920,1080),NAVY)
text(banner,960,58,'CONFIGURABLE',148,width=1760)
quality_font=text(banner,960,228,'QUALITY',275,GOLD,width=1550)
text(banner,960,531,'Choose what gets quality',57,width=1760)
text(banner,960,606,'and what quality changes.',57,width=1760)
floor=floor.convert('RGB').resize((1920,1920),Image.Resampling.NEAREST).crop((0,0,1920,360))
floor=Image.blend(floor,Image.new('RGB',floor.size,NAVY),.7)
banner.paste(floor,(0,720)); divider(banner,710)
art(banner,'wall',(170,795,405,1030)); toggle(banner,475,890,False,155,75)
art(banner,'bed',(855,775,995,1040)); toggle(banner,1080,890,True,155,75)
art(banner,'rifle',(1370,820,1695,1010)); toggle(banner,1740,890,True,155,75)
banner.save(OUT/'title-1920x1080.png')
preview=banner.resize((640,360),Image.Resampling.LANCZOS)
preview.save(ROOT/'About/Preview.png')

# Animate only an exact glyph mask. Sprites, toggles and other lettering remain still.
mask=Image.new('L',banner.size)
ImageDraw.Draw(mask).text((960,228),'QUALITY',font=font(quality_font),fill=255,anchor='mt')
mask=mask.resize(preview.size,Image.Resampling.LANCZOS)
frames=[]
for i in range(31):
    shine=Image.new('L',preview.size)
    cx=-80+800*i/30
    ImageDraw.Draw(shine).polygon([(cx-16,65),(cx+16,65),(cx+60,160),(cx+28,160)],fill=145)
    shine=ImageChops.multiply(shine.filter(ImageFilter.GaussianBlur(5)),mask)
    frames.append(Image.composite(Image.new('RGB',preview.size,'#fff5d6'),preview,shine))
palette=preview.quantize(colors=256)
frames=[frame.quantize(palette=palette,dither=Image.Dither.NONE) for frame in frames]
frames[0].save(ROOT/'About/Preview.gif',save_all=True,append_images=frames[1:],duration=[1600]+[60]*29+[1580],loop=0,disposal=2,optimize=False)
with Image.open(ROOT/'About/Preview.gif') as animation:
    assert animation.size==(640,360) and animation.n_frames>2 and animation.info['loop']==0

metadata={
    'renderer':'scripts/render-workshop-art.py',
    'font':font(40).getname()[0],
    'fontSource':'Local RimWordFont.ttf; no generated lettering; font file is not redistributed.',
    'gameAssets':'Original DDS sprites read from the local RimWorld Assets.zip; wall and bed use native material colours.',
    'objects':['Wall_Atlas_Bricks.psd.dds','Bed_south.psd.dds','Bed_southm.psd.dds','AssaultRifle.png.dds','Shelf_south.psd.dds','DoorSimple_MenuIcon.psd.dds','TableMachining_south.psd.dds','Heater.psd.dds','Silver_a.psd.dds','WoodFloor.psd.dds'],
    'assets':['quality-choices.png','quality-effects.png','quality-wealth.png','quality-rules.png','quality-strength.png','granite-wall.png','assault-rifle.png','wooden-bed.png','title-1920x1080.png','About/Preview.png','About/Preview.gif'],
    'imageGenerationUsed':False,
    'chartValues':values,
    'chartValueSources':['Source/Quality_Configuration.cs','Source/Quality_Progression.cs','Installed Core Stats_Basics_General.xml','Installed Core Stats_Building_Special.xml','Installed Core RangedIndustrial.xml'],
    'chartRounding':'HP percentages show factors before integer rounding; cooldown seconds show two decimals; native market factors precede gain caps and rounding.',
}
(OUT/'explainer-prompts.json').write_text(json.dumps(metadata,indent=2)+'\n',encoding='utf-8')
print('Rendered all public artwork with RimWordFont and original game sprites. No generated objects or lettering remain.')

