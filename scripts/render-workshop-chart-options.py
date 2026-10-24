"""Render local comparison options for the Workshop stat charts."""
import runpy
from pathlib import Path
from PIL import Image, ImageDraw
r=runpy.run_path(str(Path(__file__).with_name('render-workshop-art.py')))
text,art,font=r['text'],r['art'],r['font']
navy,panel,cream,gold=r['NAVY'],r['PANEL'],r['CREAM'],r['GOLD']
colors,tiers,benefits,market=r['COLORS'],r['tiers'],r['benefits'],r['market']
out=r['ROOT']/'artifacts/chart-alternatives'; out.mkdir(exist_ok=True)
cooldown=['2.83 s','2.13 s','1.70 s','1.24 s','0.97 s','0.80 s','0.68 s']

def save(im,name):
    im.save(out/(name+'.png'))
    im.resize((636,round(im.height*636/im.width)),Image.Resampling.LANCZOS).save(out/(name+'-preview.png'))

# A: familiar column layout with short, level headings.
im=Image.new('RGB',(1672,620),navy); d=ImageDraw.Draw(im)
start=300; cell=(1672-start)/7
text(im,25,32,'Quality',36,anchor='lt')
for i,tier in enumerate(['Awful','Poor','Normal','Good','Excel.','Master.','Legend.']):
    x=start+i*cell; cx=x+cell/2
    if i==2:d.rectangle((x,15,x+cell,600),fill=panel)
    text(im,cx,32,tier,32,colors[i])
    art(im,'rifle',(x+14,110,x+cell-14,230))
    d.rectangle((x+14,255,x+cell-14,261),fill=colors[i])
    for row,value in enumerate([f'{benefits[i]:g}',cooldown[i],f'{benefits[i]*100:g}%',f'{market[i]:g}x']):
        y=305+row*78
        if row==0:d.rounded_rectangle((x+14,y-8,x+cell-14,y+51),radius=4,outline='#6a818b',width=2)
        text(im,cx,y,value,40,width=cell-15)
for row,label in enumerate(['Scaling','Cooldown','Max. HP','Market']):
    text(im,25,305+row*78,label,35,anchor='lt')
text(im,25,343,'Factor',35,anchor='lt');text(im,25,577,'value',35,anchor='lt')
save(im,'a-short-headings')

# B: numbered tiers give the columns room; a full-name key sits below.
im=Image.new('RGB',(1672,680),navy);d=ImageDraw.Draw(im)
for i,tier in enumerate(tiers):
    x=start+i*cell;cx=x+cell/2
    if i==2:d.rectangle((x,15,x+cell,510),fill=panel)
    text(im,cx,26,str(i+1),48,colors[i])
    art(im,'rifle',(x+14,92,x+cell-14,205))
    d.rectangle((x+14,227,x+cell-14,233),fill=colors[i])
    for row,value in enumerate([f'{benefits[i]:g}',cooldown[i],f'{benefits[i]*100:g}%',f'{market[i]:g}x']):
        text(im,cx,270+row*65,value,40,width=cell-15)
    key_x=35+(i%4)*415;key_y=550+(i//4)*67
    text(im,key_x,key_y,f'{i+1} {tier}',34,colors[i],anchor='lt')
for row,label in enumerate(['Scaling factor','Cooldown','Max. HP','Market value']):
    # Wrap long stat labels without reducing type size.
    parts=label.split() if label in ['Scaling factor','Market value'] else [label]
    for j,part in enumerate(parts):text(im,25,265+row*65+j*32,part,32,anchor='lt')
save(im,'b-numbered-tiers')

# C: transpose the data so full tier names stay horizontal and equally sized.
im=Image.new('RGB',(1672,760),navy);d=ImageDraw.Draw(im)
art(im,'rifle',(45,16,260,100))
xs=[90,655,970,1250,1530]
for x,lines in zip(xs[1:],[['Scaling','factor'],['Cooldown'],['Max. HP'],['Market','value']]):
    for j,line in enumerate(lines):text(im,x,25+j*40,line,36)
for i,tier in enumerate(tiers):
    y=130+i*86
    if i==2:d.rectangle((25,y-10,1647,y+68),fill=panel)
    d.rectangle((30,y-6,39,y+64),fill=colors[i])
    text(im,65,y+7,tier,39,colors[i],anchor='lt')
    for x,value in zip(xs[1:],[f'{benefits[i]:g}',cooldown[i],f'{benefits[i]*100:g}%',f'{market[i]:g}x']):text(im,x,y+7,value,40)
    d.line((45,y+73,1627,y+73),fill='#2d4551',width=1)
save(im,'c-full-name-rows')
print('Created three compact chart alternatives in '+str(out))
