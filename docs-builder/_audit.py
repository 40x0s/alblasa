import pypdfium2 as pdfium
doc = pdfium.PdfDocument("../Crack_Guide_x32dbg.pdf")
sc = 2.0; mm = 72/25.4*sc
L, R, T, B = int(14.5*mm), int((210-14.5)*mm), int(8*mm), int((297-12)*mm)
def dark(im, box):
    h = im.convert("L").crop(box).histogram()
    return sum(h[:170])
bad = []
for i, pg in enumerate(doc):
    im = pg.render(scale=sc).to_pil()
    w, ht = im.size
    lt, rt, bt = dark(im,(0,0,L,ht)), dark(im,(R,0,w,ht)), dark(im,(0,B,w,ht))
    if lt > 30 or rt > 30 or bt > 30:
        bad.append((i+1, lt, rt, bt))
print("suspects:", bad if bad else "none — all ink inside margins")
