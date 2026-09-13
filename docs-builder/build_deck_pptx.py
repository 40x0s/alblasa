#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
build_deck_pptx.py — يولّد ../MizanPro_Crack_Workshop.pptx (عرض 22 شريحة، عربي RTL).

    python3 docs-builder/build_deck_pptx.py

التصميم: كحلي #0B1D34 + ذهبي #C59421، خط Segoe UI (موجود على Windows)، كل فقرة
rtl="1" + محاذاة يمين، وملاحظات المتحدث مكتوبة داخل كل شريحة (Notes).
"""
import os
import copy

from pptx import Presentation
from pptx.util import Inches, Pt, Emu
from pptx.dml.color import RGBColor
from pptx.enum.text import PP_ALIGN, MSO_ANCHOR
from pptx.oxml.ns import qn

OUT = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "MizanPro_Crack_Workshop.pptx"))

NAVY = RGBColor(0x0B, 0x1D, 0x34)
NAVY2 = RGBColor(0x14, 0x2C, 0x4C)
GOLD = RGBColor(0xC5, 0x94, 0x21)
INK = RGBColor(0x1A, 0x20, 0x2A)
GRAY = RGBColor(0x6C, 0x7C, 0x8C)
WHITE = RGBColor(0xFF, 0xFF, 0xFF)
SOFT = RGBColor(0xF6, 0xF8, 0xFB)
CODE_BG = RGBColor(0x14, 0x1D, 0x2C)
CODE_FG = RGBColor(0xE2, 0xEC, 0xF8)
RED = RGBColor(0xB8, 0x24, 0x24)
GREEN = RGBColor(0x12, 0x78, 0x46)
AR_FONT = "Segoe UI"
MONO_FONT = "Consolas"

W, H = Inches(13.333), Inches(7.5)


# ─────────────────────────── أدوات ───────────────────────────
def rtl(p, align="r", level=0, rtl_on=True):
    """يضبط الاتجاه من XML: محاذاة يمين + rtl + مستوى تداخل."""
    pPr = p._pPr if p._pPr is not None else p._p.get_or_add_pPr()
    pPr.set("algn", align)
    pPr.set("marL", str(Emu(Inches(0.28 * level))))
    pPr.set("indent", "0")
    if rtl_on:
        pPr.set("rtl", "1")
    # تأكد من وجود a:defRPr مع rtl للمحاذاة النقية في بعض النسخ
    for r in p.runs:
        rPr = r._r.get_or_add_rPr()
        rPr.set("lang", "ar-SA")
        if rtl_on:
            rPr.set("algn", align)


def box(slide, x, y, w, h, fill=None, line=None, lw=1.0, shadow=False, radius=None):
    from pptx.enum.shapes import MSO_SHAPE
    shp = slide.shapes.add_shape(
        MSO_SHAPE.ROUNDED_RECTANGLE if radius else MSO_SHAPE.RECTANGLE, x, y, w, h)
    shp.text_frame.word_wrap = True
    if fill is None:
        shp.fill.background()
    else:
        shp.fill.solid()
        shp.fill.fore_color.rgb = fill
    if line is None:
        shp.line.fill.background()
    else:
        shp.line.color.rgb = line
        shp.line.width = Pt(lw)
    shp.shadow.inherit = False
    return shp


def text(slide, x, y, w, h, runs, size=18, bold=False, color=INK, align="r",
         font=AR_FONT, anchor=MSO_ANCHOR.TOP, line_spacing=1.0, space_after=6):
    """runs: str أو [(txt, dict-override)]."""
    tb = slide.shapes.add_textbox(x, y, w, h)
    tf = tb.text_frame
    tf.word_wrap = True
    tf.vertical_anchor = anchor
    tf.margin_left = tf.margin_right = Inches(0.02)
    tf.margin_top = tf.margin_bottom = 0
    if isinstance(runs, str):
        runs = [(runs, {})]
    p = tf.paragraphs[0]
    p.line_spacing = line_spacing
    p.space_after = Pt(space_after)
    for txt, ov in runs:
        r = p.add_run()
        r.text = txt
        f = r.font
        f.size = Pt(ov.get("size", size))
        f.bold = ov.get("bold", bold)
        f.name = ov.get("font", font)
        f.color.rgb = ov.get("color", color)
        if ov.get("mono"):
            f.name = MONO_FONT
            f.size = Pt(ov.get("size", size - 3))
    rtl(p, align)
    return tb


def para_list(slide, x, y, w, h, items, size=17, color=INK, bullet="•", gap=9,
              align="r", line_spacing=1.08):
    """قائمة نقاط/أسطر متعددة في صندوق واحد، كل سطر فقرته (RTL)."""
    tb = slide.shapes.add_textbox(x, y, w, h)
    tf = tb.text_frame
    tf.word_wrap = True
    tf.margin_left = tf.margin_right = Inches(0.02)
    tf.margin_top = tf.margin_bottom = 0
    first = True
    for it in items:
        lvl = 0
        if isinstance(it, tuple):
            it, lvl = it
        p = tf.paragraphs[0] if first else tf.add_paragraph()
        first = False
        p.space_after = Pt(gap)
        p.line_spacing = line_spacing
        mark = ("" if bullet == "" else ("–  " if lvl else bullet + "  "))
        segs = []
        buf = it
        # دعم **عريض** و`كود` بشكل مبسّط
        while "`" in buf:
            a, rest = buf.split("`", 1)
            if "`" not in rest:
                break
            b, buf = rest.split("`", 1)
            if a:
                segs.append((a, {}))
            segs.append((b, {"mono": True, "color": GOLD if color == WHITE else RGBColor(0x8A, 0x5F, 0x0A), "bold": True}))
        if "**" in buf:
            parts = buf.split("**")
            for i2, seg in enumerate(parts):
                if seg:
                    segs.append((seg, {"bold": i2 % 2 == 1}))
        elif buf:
            segs.append((buf, {}))
        if mark:
            p.add_run().text = mark
            r = p.runs[0]
            r.font.size = Pt(size)
            r.font.color.rgb = GOLD
            r.font.name = AR_FONT
            r.font.bold = True
        for txt, ov in segs:
            r = p.add_run()
            r.text = txt
            r.font.size = Pt(ov.get("size", size - (1 if lvl else 0)))
            r.font.bold = ov.get("bold", False)
            r.font.name = ov.get("font", MONO_FONT if ov.get("mono") else AR_FONT)
            r.font.color.rgb = ov.get("color", GRAY if lvl else color)
        rtl(p, align, level=lvl)
    return tb


def code_box(slide, x, y, w, h, lines, size=13, title=None):
    shp = box(slide, x, y, w, h, fill=CODE_BG, line=GOLD, lw=1.0)
    tf = shp.text_frame
    tf.word_wrap = True
    tf.margin_left = tf.margin_right = Inches(0.16)
    tf.margin_top = Inches(0.12)
    tf.vertical_anchor = MSO_ANCHOR.TOP
    i = 0
    if title:
        p = tf.paragraphs[0]
        r = p.add_run()
        r.text = title
        r.font.size = Pt(size - 2)
        r.font.name = MONO_FONT
        r.font.bold = True
        r.font.color.rgb = RGBColor(0x8C, 0xAC, 0xD0)
        p.space_after = Pt(6)
        i = 1
    for n, ln in enumerate(lines, i):
        p = tf.paragraphs[0] if n == 0 else tf.add_paragraph()
        r = p.add_run()
        r.text = ln
        r.font.size = Pt(size)
        r.font.name = MONO_FONT
        r.font.color.rgb = RGBColor(0x7C, 0xC4, 0x9C) if ln.lstrip().startswith(("#", "::", "REM")) else CODE_FG
        p.space_after = Pt(2)
        p.line_spacing = 1.02
        rtl(p, "l", rtl_on=False)
    return shp


def slide_frame(prs, kicker, title, sub=None, dark=False):
    s = prs.slides.add_slide(prs.slide_layouts[6])
    box(s, 0, 0, W, H, fill=NAVY if dark else WHITE)
    if not dark:
        box(s, 0, 0, W, Inches(0.10), fill=GOLD)
        # شريط علوي: ترقيم + اسم العرض
        text(s, Inches(0.5), Inches(0.26), Inches(6.5), Inches(0.3), kicker,
             size=12, color=GRAY, align="l", font=MONO_FONT)
        text(s, W - Inches(7.6), Inches(0.20), Inches(7.1), Inches(0.62), title,
             size=30, bold=True, color=NAVY)
        y = Inches(0.92)
        box(s, W - Inches(0.5) - Inches(3.1), y - Inches(0.02), Inches(3.1), Pt(3.2), fill=GOLD)
        if sub:
            text(s, W - Inches(7.6), y + Inches(0.08), Inches(7.1), Inches(0.4), sub,
                 size=14.5, color=GRAY)
        return s, Inches(1.42)
    text(s, Inches(0.5), Inches(0.34), Inches(6.0), Inches(0.3), kicker,
         size=12, color=GOLD, align="l", font=MONO_FONT)
    text(s, W - Inches(9.4), Inches(1.5), Inches(8.9), Inches(1.4), title,
         size=40, bold=True, color=WHITE)
    if sub:
        text(s, W - Inches(9.4), Inches(2.7), Inches(8.9), Inches(0.6), sub, size=17, color=RGBColor(0xB8, 0xCB, 0xE2))
    box(s, W - Inches(3.5), Inches(2.45), Inches(3.0), Pt(3.5), fill=GOLD)
    return s, Inches(3.6)


def notes(slide, txt):
    slide.notes_slide.notes_text_frame.text = txt


def tag(slide, x, y, w, label, color=GOLD, fg=WHITE, size=12):
    shp = box(slide, x, y, w, Inches(0.34), fill=color)
    tf = shp.text_frame
    tf.margin_top = tf.margin_bottom = 0
    tf.vertical_anchor = MSO_ANCHOR.MIDDLE
    p = tf.paragraphs[0]
    r = p.add_run()
    r.text = label
    r.font.size = Pt(size)
    r.font.bold = True
    r.font.name = AR_FONT
    r.font.color.rgb = fg
    rtl(p, "ctr")
    return shp


# ─────────────────────────── الشرائح ───────────────────────────
def build(prs):
    # 1 — الغلاف
    s = prs.slides.add_slide(prs.slide_layouts[6])
    box(s, 0, 0, W, H, fill=NAVY)
    box(s, 0, H - Inches(1.5), W, Inches(0.06), fill=GOLD)
    text(s, W - Inches(10.4), Inches(0.9), Inches(9.9), Inches(0.5),
         "المشروع العملي — الهندسة العكسية · دفعة 2026", size=16, color=RGBColor(0xA9, 0xC2, 0xDC))
    text(s, W - Inches(11.6), Inches(1.55), Inches(11.1), Inches(2.2),
         "كسر حماية ميزان Pro", size=54, bold=True, color=WHITE)
    text(s, W - Inches(11.6), Inches(2.85), Inches(11.1), Inches(1.0),
         [("بـ ", {}), ("x32dbg", {"font": MONO_FONT, "color": GOLD, "bold": True, "size": 30}),
          (" — من الصفر إلى المية", {})], size=30, color=RGBColor(0xD6, 0xE4, 0xF2))
    box(s, W - Inches(4.4), Inches(3.75), Inches(3.9), Pt(3.5), fill=GOLD)
    para_list(s, W - Inches(11.4), Inches(4.15), Inches(10.9), Inches(1.9), [
        "الفريق: **علي العطيفي** · **ماجد الدميني** · **المعتصم الراعي**",
        "الإشراف الأكاديمي: **م. عبدالكريم الباشيري**",
        "الهدف: `MizanPro.exe` — نظام محاسبة WPF بنيناه **بأنفسنا** ليكون **crackme** تعليمياً",
        "مدة العرض: **38 دقيقة** + تطبيق مباشر + إشراف على 3 مجموعات",
    ], size=17, color=RGBColor(0xC9, 0xDA, 0xEC), gap=6)
    text(s, Inches(0.55), H - Inches(1.25), Inches(9.4), Inches(0.8),
         "تحذير: كل ما سيُعرض على برنامج من بنائنا ولغرض تعليمي — لا يُطبَّق ولا يُنشَر على برامج الغير.",
         size=13, color=RGBColor(0x93, 0xA8, 0xBF))
    notes(s, "افتح بجملة واحدة: «اخترنا أن نكسر برنامجنا نحن، لا برنامجاً ننزّله من الإنترنت — لأن المادة تطلب فهم الآلية لا التحميل». "
             "قدّم الفريق في 20 ثانية، وانتقل. لا تبدأ بالأدوات.")

    # 2 — المطلوب وكيف ترجمناه
    s, y = slide_frame(prs, "01 · BRIEF", "ما الذي طُلب منّا؟", "كيف قرأنا رسالة المشرف، وماذا نفّذنا")
    rows = [("المطلوب", "ما نفّذناه", "أين تراه اليوم"),
            ("كل مجموعة تختار برنامجاً أو برمجية خبيثة",
             "برنامج محاسبة كامل (WPF/.NET 6) كتبناه نحن، وحماية ترخيص من 3 طبقات", "README.md + الشجرة"),
            ("كسر الحماية **أو** تحليل سلوكي", "الاثنان معاً: رتق IL (كسر) + تتبّع Registry/رسائل API (سلوكي)", "الفصول 6–9 من الدليل"),
            ("عرض 30–40 دقيقة + تطبيق مباشر", "10 خطوات دقيقة أمام الجمهور، كلها مُجرَّبة ومُسجَّلة احتياطياً", "هذه الشرائح + demo.mp4"),
            ("الإشراف على مجموعات تفعل الشيء نفسه", "قائمة تحقّق + سلم تقدير + 3 أخطاء نتوقعها منهم", "شريحة «كيف نديرهم»")]
    x = W - Inches(0.6) - Inches(7.9)
    for i, r in enumerate(rows):
        ty = y + Inches(i * 0.92)
        box(s, x, ty, Inches(7.9), Inches(0.86), fill=(NAVY if i == 0 else (SOFT if i % 2 else WHITE)),
            line=RGBColor(0xE0, 0xE7, 0xEF), lw=0.75)
    for i, r in enumerate(rows):
        ty = y + Inches(i * 0.92)
        text(s, x + Inches(5.55), ty + Inches(0.16), Inches(2.25), Inches(0.6), r[0],
             size=13.5, bold=(i == 0), color=WHITE if i == 0 else NAVY)
        text(s, x + Inches(2.05), ty + Inches(0.13), Inches(3.35), Inches(0.7), r[1],
             size=12.5, bold=(i == 0), color=WHITE if i == 0 else INK)
        text(s, x - Inches(0.05), ty + Inches(0.16), Inches(2.0), Inches(0.6), r[2],
             size=12, bold=(i == 0), color=GOLD if i == 0 else GRAY)
    tag(s, Inches(0.62), y + Inches(4.7), Inches(4.4), "وثّقنا كل خطوة في PDF من 51 صفحة وُزّع قبل الورشة", NAVY2)
    notes(s, "لا تقرأ الجدول حرفياً. قل: «المشرف طلب اختيار هدف، واختيار الطريقة. نحن اخترنا الهدف الذي نستطيع الدفاع عن أخلاقيته، "
             "ووزّعنا العمل: تحليل، ثم كسر، ثم دفاع». ثم اذكر أن الـ PDF هو «ورقة الغش» الرسمية للطلاب.")

    # 3 — الهدف: ميزان Pro
    s, y = slide_frame(prs, "02 · TARGET", "ميزان Pro: برنامج محاسبة يُخفي ثلاث طبقات حماية",
                       "13 صفحة، 5 آلاف سطر، وحماية «الترخيص حالة لا شاشة»")
    para_list(s, W - Inches(7.0), y + Inches(0.1), Inches(6.5), Inches(3.5), [
        "واجهة عربية RTL: لوحة تحكم، فواتير، منتجات، عملاء، تقارير",
        "طبقة 1 — **مفتاح ترخيص**: 20 محرفاً `[A-Z0-9]` بشرطين حسابيين",
        "طبقة 2 — **حالة مخزَّنة** في `HKCU\…\RuntimeState`",
        "طبقة 3 — **ملف احتياطي** `state.enc` (AES-128 + بصمة الجهاز)",
        "تجربة 7 أيام من `InstallDate` — تُقرأ مرة واحدة عند الإقلاع",
        "لا خادم، لا توقيع، لا فحص تكامل — **عمداً**: هدف تعليمي",
    ], size=15.5, gap=8)
    box(s, Inches(0.62), y + Inches(0.1), Inches(4.75), Inches(3.5), fill=NAVY, radius=True)
    labels = [("مفتاح الترخيص", "صيغة + حساب", RGBColor(0x1F, 0x3C, 0x63)),
              ("ActivationBlob", "قيمة في Registry", RGBColor(0x27, 0x4A, 0x74)),
              ("state.enc", "ملف مشفّر بمفتاح ثابت", RGBColor(0x2E, 0x59, 0x88)),
              ("EvaluateCurrentState", "بوابة الإقلاع → PRO / تجربة", GOLD)]
    for i, (t, d, c) in enumerate(labels):
        by = y + Inches(0.32) + Inches(i * 0.78)
        box(s, Inches(0.92), by, Inches(4.15), Inches(0.62), fill=c, radius=True)
        text(s, Inches(1.1), by + Inches(0.05), Inches(3.9), Inches(0.3), t,
             size=14, bold=True, color=WHITE, align="r", font=(MONO_FONT if i == 3 else AR_FONT))
        text(s, Inches(1.1), by + Inches(0.30), Inches(3.9), Inches(0.28), d,
             size=11.5, color=RGBColor(0xCB, 0xDD, 0xF0), align="r")
    text(s, Inches(0.62), y + Inches(3.7), Inches(4.75), Inches(0.5),
         "كل طبقة أضعف مما تبدو — وهذا هو المشروع كله", size=14, bold=True, color=RED)
    notes(s, "اشرح «الترخيص حالة، وليس شاشة»: البرنامج لا يسأل الخادم ولا يطابق بصمته مع المفتاح؛ يقرأ ما كُتب له ويصدّقه. "
             "هذه الجملة ستعود في كل كسر من الكسور الثلاثة.")

    # 4 — القرار في دالة واحدة
    s, y = slide_frame(prs, "03 · THE DECISION", "كل حماية = فحص + قرار", "وأين القرار في MizanPro؟ هنا بالضبط")
    code_box(s, W - Inches(7.55), y + Inches(0.05), Inches(7.05), Inches(2.55), [
        "// Core/Engine/TokenProcessor.cs",
        "int  sum = clean.Sum(c => (int)c);",
        "byte xor = 0;",
        "foreach (char c in clean) xor ^= (byte)c;",
        "",
        "return sum % 73 == 0 && xor == RequiredXor;   // RequiredXor = 0x0A (const)",
    ], size=13.5, title="دالة التحقق كاملة — 5 أسطر تُحدّد كل شيء")
    para_list(s, Inches(0.62), y + Inches(0.15), Inches(4.45), Inches(2.5), [
        "**قرار واحد** في `ParseAndVerify` — كل الطبقات الأخرى تصدّق نتيجته",
        "لا بصمة جهاز في المفتاح، ولا تاريخ صلاحية",
        "فحصان «ضعيفان» تعمداً: قابلان **للاشتقاق**",
    ], size=15, gap=8)
    box(s, W - Inches(7.55), y + Inches(2.75), Inches(7.05), Inches(1.3), fill=SOFT, line=GOLD, lw=1.5)
    text(s, W - Inches(7.3), y + Inches(2.85), Inches(6.6), Inches(1.1),
         [("(القاعدة الذهبية) ", {"bold": True, "color": RED}),
          ("«أين يُتَّخذ القرار؟» — كل كسر حقيقي هو جملة واحدة: ", {}),
          ("«القرار في الدالة X ← سأجعل X تُرجع ما أريد»", {"bold": True, "color": NAVY})], size=16, align="r")
    tag(s, Inches(0.62), y + Inches(3.0), Inches(4.45), "الفصل 5 + 7 في الـ PDF — التفاصيل الحرفية", NAVY2)
    notes(s, "هنا تبدأ اللحظة الأهم: اقرأ السطر الأخير بصوت عالٍ، ثم قل «سنغيّر رقماً واحداً في الذاكرة، وهذا السطر يسقط». "
             "لا تُفصّل البايتات الآن — احفظها للشريحة 9.")

    # 5 — الأدوات
    s, y = slide_frame(prs, "04 · TOOLING", "الأدوات — ولماذا x32dbg هو النجم", "كل أداة لها سؤال تجيب عليه، لا فضول")
    tools = [("x32dbg", "نقاط توقف، ذاكرة، سجلات، رتق بايتات — **يرى ما ينفّذه المعالج**", GOLD),
             ("dnSpyEx", "شجرة IL وC# كاملة + تعديل جسم الطريقة وحفظ الملف", NAVY2),
             ("HxD", "بحث/تعديل على القرص — الرتق الدائم", NAVY2),
             ("Procmon", "إثبات «ماذا كُتب فعلاً» في Registry/الملفات", NAVY2),
             ("Python + pycryptodome", "AES-CBC لتزوير `state.enc`، ومولّد المفاتيح", NAVY2)]
    for i, (t, d, c) in enumerate(tools):
        by = y + Inches(i * 0.82)
        box(s, Inches(0.62), by, Inches(12.1), Inches(0.7), fill=WHITE, line=RGBColor(0xE2, 0xE9, 0xF1), lw=0.75)
        box(s, Inches(12.2), by, Inches(0.52), Inches(0.7), fill=c)
        text(s, W - Inches(3.9), by + Inches(0.16), Inches(3.1), Inches(0.4), t,
             size=17, bold=True, color=NAVY, font=(AR_FONT if t[0].isupper() or t[0].isalpha() and t[0].islower() and len(t) < 9 else MONO_FONT))
        text(s, Inches(0.9), by + Inches(0.20), Inches(10.6), Inches(0.4), d, size=14.5, color=INK)
    text(s, Inches(0.62), y + Inches(4.25), Inches(12.1), Inches(0.55),
         [("⚠ ", {"color": RED, "bold": True}),
          ("x32dbg وحده لا «يفهم» .NET: لا أسماء دوال، ولا IL — ولهذا نستخدمه للطابق الذي يبرع فيه، وdnSpy للطابق المُدار. ", {}),
          ("اختيار الأداة نصف الحل.", {"bold": True, "color": NAVY})], size=15)
    notes(s, "هذه الشريحة تمنع السؤال المحرج «ليش ما كسرتم بدنبق فقط؟». الجواب: الأدوات مكمّلة — IL في dnSpy، وذاكرة/سجلات في x32dbg. "
             "وأنت تعرض الرتق تحت x32dbg لأنه يري الطلاب أن الكسر «حدث حقيقي» في عملية حيّة.")

    # 6 — .NET يغيّر القواعد
    s, y = slide_frame(prs, "05 · WHY .NET IS DIFFERENT", "‏IL + JIT: التوقيت هو الحكم",
                       "أهم شريحة تعليمياً — وأصعبها على الطلاب")
    para_list(s, W - Inches(7.0), y + Inches(0.1), Inches(6.5), Inches(2.9), [
        "C# ← **IL** داخل `MizanPro.dll` ← **JIT** يولّد كود الآلة عند أول استخدام",
        "`TieredCompilation`: تُترجم سريعاً أولاً (tier0) ثم قد تُعاد (tier1)",
        "⚠ رتق IL بعد أن تعمل الدالة = **لا شيء يتغيّر**",
        "لهذا نرتق **قبل** أول محاولة تفعيل — أو نرتق الملف على القرص",
        "منقّح native يرى تعليمات مُترجَمة، لا الطريقة التي كتبتها",
    ], size=16.5, gap=9)
    box(s, Inches(0.62), y + Inches(0.05), Inches(5.5), Inches(3.15), fill=SOFT, line=NAVY, lw=1.25)
    seq = ["① نشر بلا ملف واحد → `MizanPro.dll` موجود",
           "② F3: تشغيل `MizanPro.exe` تحت x32dbg",
           "③ F9 حتى تظهر نافذة التطبيق، ثم F12",
           "④ Alt+M → `MizanPro.dll` → Ctrl+B → `1F 49 D0`",
           "⑤ Ctrl+E: غيّر `49` ← `01` (بايت واحد)",
           "⑥ F9 ← ثم الصق المفتاح «المُثبِت» ← **تم التفعيل**"]
    for i, t in enumerate(seq):
        text(s, Inches(0.86), y + Inches(0.2 + i * 0.46), Inches(5.05), Inches(0.42), t, size=13.5, color=NAVY, align="r")
    box(s, W - Inches(7.0), y + Inches(3.2), Inches(6.5), Inches(0.75), fill=NAVY, radius=True)
    text(s, W - Inches(6.85), y + Inches(3.33), Inches(6.2), Inches(0.5),
         "جرّبناها بالمقلوب عمداً: أعدنا التجربة بعد أن عملت الدالة ← رُفض المفتاح. هذا هو الدليل.",
         size=14, bold=True, color=WHITE)
    notes(s, "اطلب من الجمهور التخمين قبل أن تُظهر الشريحة: «لو عدّلت IL بعد ما جرّبت المفتاح، يشتغي؟». "
             "90% يقولون نعم — هنا تبدأ المحاضرة الفعلية. الفصل 2.٢ و7.١ من الـ PDF فيهما البرهان.")

    # 7 — الكسر 1
    s, y = slide_frame(prs, "06 · CRACK №1", "الكسر الأول: زوّر الطبقة الثانية", "بلا منقِّح، بلا رتق — 4 أوامر")
    code_box(s, W - Inches(7.6), y + Inches(0.05), Inches(7.1), Inches(2.25), [
        "reg add \"HKCU\\Software\\MizanSolutions\\Mizan\\RuntimeState\" \\",
        "        /v ActivationBlob /t REG_SZ /d M1Z4N-2025P-ROM4S-T3R01 /f",
        "",
        "python forge_state.py 1A2B-3C4D-5E6F-7A8B   # يكتب state.enc مزوَّراً",
        "#  AES-128-CBC(\"MIZAN_VALID|<بصمة الجهاز>\")  بمفتاح من الكود المصدري",
    ], size=12.5, title="cmd + python — 30 ثانية")
    para_list(s, Inches(0.62), y + Inches(0.1), Inches(4.5), Inches(2.2), [
        "البرنامج **يصدّق ما كُتب له**: لا توقيع ولا ربط بالمستخدم",
        "`state.enc` يشفيه بمفتاح `Mizan2024_K3y128` **مكتوب في نفس الحزمة**",
        "المسار الاحتياطي يسبق فحص تاريخ التجربة ⇒ يعمل حتى بعد انتهائها",
    ], size=14.5, gap=8)
    for i, (lbl, txt) in enumerate([("الدليل (أ)", "قبل: «انتهت التجربة» — بعد: اللافتة PRO دون أي رتق"),
                                    ("الدليل (ب)", "`reg query` يُظهر القيمة؛ احذفها ← يعود الوضع التجريبي")]):
        box(s, Inches(0.62) if i else Inches(0.62), y + Inches(2.45 + i * 0.62), Inches(4.5), Inches(0.55),
            fill=SOFT, line=GOLD, lw=1.0)
        text(s, Inches(0.8), y + Inches(2.52 + i * 0.62), Inches(4.15), Inches(0.45),
             [(lbl + ": ", {"bold": True, "color": GOLD}), (txt, {})], size=12.5, color=INK)
    tag(s, W - Inches(7.6), y + Inches(2.5), Inches(7.1), "هذا ليس «كسر الخوارزمية» — هذا «كسر ثقة». ورسالة للمشرف: لا تخزّن قرارك بشكل قابل للتحرير", NAVY2)
    notes(s, "أكّد على الفرق: في الطبقة الأولى نرضي الحساب، هنا **نتجاوز الحساب** لأن مصدر الثقة خارجي وقابل للكتابة. "
             "اسأل: «كيف تمنعون هذا في مشروعكم؟» ← الجواب في شريحة الدفاع.")

    # 8 — الكسر 2: الإعداد
    s, y = slide_frame(prs, "07 · CRACK №2 — THE STAR", "‏x32dbg داخل `MizanPro.dll`: بايت واحد",
                       "لا ملف يتغيّر، ولا Antivirus يشتكي، ولا إعادة تشغيل مطلوبة")
    code_box(s, W - Inches(6.35), y + Inches(0.05), Inches(5.85), Inches(2.05), [
        "1F 49 D0   ;  ldc.i4.s 73 ; rem     ← sum % 73",
        "2C 0E      ;  brfalse.s  FALSE      ← اقفز لو ≠ 0",
        "",
        "1F 01 D0   ;  ldc.i4.s  1 ; rem     ← sum % 1 == 0  ✓ دائماً",
        "2C 0E      ;  brfalse.s  FALSE      ← لم يعد يُشتق إليه أبداً",
    ], size=13, title="النمط المستهدف في تيار #Text")
    para_list(s, Inches(0.62), y + Inches(0.1), Inches(5.9), Inches(2.0), [
        "نبحث **ثنائياً** عن `1F 49 D0` داخل منطقة `MizanPro.dll` (Ctrl+B)",
        "نعدّل البت الأوسط في محرّر الذاكرة (Ctrl+E) — بلا تجميع، بلا إعادة بناء",
        "الرتق على **صورة الذاكرة**: الملف على القرص سليم (0 تغييرات → 0 كشف)",
    ], size=15, gap=8)
    steps = ["F3 ← `MizanPro.exe`", "F9 × 2 ← تظهر النافذة", "F12 ← إيقاف مؤقت",
             "Alt+M ← `MizanPro.dll`", "Ctrl+B ← `1F 49 D0`", "Ctrl+E ← `49`→`01`",
             "Ctrl+P ← الرتق مسجَّل", "F9 ← ثم المفتاح"]
    for i, st in enumerate(steps):
        cx = Inches(0.62) + Inches(i % 4 * 3.06)
        cy = y + Inches(2.35 + (i // 4) * 0.66)
        box(s, cx, cy, Inches(2.9), Inches(0.56), fill=(NAVY if i in (4, 5) else SOFT), line=GOLD if i in (4, 5) else None, lw=1.0, radius=True)
        text(s, cx + Inches(0.14), cy + Inches(0.13), Inches(2.65), Inches(0.4),
             [(f"{i+1}. ", {"color": GOLD, "bold": True, "font": MONO_FONT, "size": 12}), (st, {})],
             size=13, bold=True, color=WHITE if i in (4, 5) else NAVY)
    notes(s, "الخطوات الست الأولى تُنفَّذ في 40 ثانية. إن لم تظهر نتيجة للبحث: لا تتردد — قفز إلى الخطة B (الرتق على القرص بـ HxD، شريحة 11). "
             "المفتاح الذي ستلصقه: `5NOFK-JQB1Z-7HSHF-NOP6D`.")

    # 9 — الكسر 2: الدليل
    s, y = slide_frame(prs, "08 · PROOF", "الدليل السببي — ثلاث لقطات", "قبل / بعد / بعدُ الإغلاق")
    tri = [("قبل الرتق", "نفس المفتاح `5NOFK-…`\n«مفتاح الترخيص غير صالح»", RED),
           ("بعد بايت واحد", "«تم تفعيل ميزان Pro بنجاح!»\nاللافتة PRO + `ActivationBlob` كُتب", GREEN),
           ("بعد إغلاق x32dbg", "نفس المفتاح **يُرفض** من جديد\n(الملف على القرص لم يُمسّ)", NAVY)]
    for i, (t, d, c) in enumerate(tri):
        bx = W - Inches(0.62) - Inches(4.05 * (3 - i)) - Inches(0.05 * i)
        box(s, bx, y + Inches(0.1), Inches(3.95), Inches(2.35), fill=WHITE, line=c, lw=1.75)
        box(s, bx, y + Inches(0.1), Inches(3.95), Inches(0.42), fill=c)
        text(s, bx + Inches(0.2), y + Inches(0.16), Inches(3.5), Inches(0.35), t, size=14.5, bold=True, color=WHITE)
        text(s, bx + Inches(0.2), y + Inches(0.72), Inches(3.55), Inches(1.5), d, size=13, color=INK, line_spacing=1.2)
    box(s, W - Inches(12.1), y + Inches(2.6), Inches(12.1), Inches(0.95), fill=SOFT, line=GOLD, lw=1.25)
    text(s, W - Inches(11.85), y + Inches(2.72), Inches(11.6), Inches(0.75),
         [("لماذا هذا «كسر» وليس «خدعة شاشة»؟ ", {"bold": True, "color": NAVY}),
          ("لأننا غيّرنا **معيار القبول** نفسه — أي مفتاح بنفس الصيغة صار مقبولاً، مع بقاء فحوص الطول والأبجدية تعمل. "
           "ولو سُئلت: «والمفتاح الطويل 21؟» ← ما زال مرفوضاً، وهذا يثبت أنك لم تُعطّل الدالة كلها.", {})], size=14.5)
    notes(s, "اقرأ الأرقام: 1426 = 73×19 + 39 ⇒ الشرط الأول يفشل، وXOR = 0x0A ⇒ الشرط الثاني ينجح. لهذا المفتاح بالذات "
             "هو «المفتاح المُثبِت للرتق» — لا يمكن أن يُقبل بغيره.")

    # 10 — درس التوقيت
    s, y = slide_frame(prs, "09 · THE TRAP", "التجربة العكسية: لماذا رُفض المفتاح؟",
                       "الفخ الذي يقع فيه 8 من 10 طلاب — ونحن صوّرناه")
    para_list(s, W - Inches(7.0), y + Inches(0.1), Inches(6.5), Inches(2.5), [
        "التسلسل: فشل ← F12 ← رتق ← إعادة المحاولة",
        "النتيجة: **ما زال مرفوضاً** — رغم أن الـ Patches تعرض الرتق!",
        "السبب: `ParseAndVerify` تُرجمت إلى كود آلة من نسخة **IL القديمة**",
        "العلاج: أعد التشغيل (Ctrl+F2) ورتق **قبل** أول استخدام",
        "البديل الأكيد: رتق الملف على القرص (HxD/dnSpy)",
    ], size=14.5, gap=6)
    box(s, Inches(0.62), y + Inches(0.1), Inches(5.55), Inches(2.5), fill=NAVY, radius=True)
    text(s, Inches(0.9), y + Inches(0.28), Inches(5.0), Inches(0.5), "الخلاصة التي تُكتب في التقرير",
         size=15, bold=True, color=GOLD)
    text(s, Inches(0.9), y + Inches(0.72), Inches(5.0), Inches(1.7),
         "«في بيئة مُدارة، تعديل الصورة المُدارة لا يكفي إن كانت الطريقة قد تُرجمت مسبقاً. التوقيت جزء من التهديد، "
         "والنسخة المُعدّة للعرض يجب أن تُبنى بحيث تُطبَّق الرتقة قبل أول استدعاء — أو تُطبَّق على القرص».",
         size=14, color=WHITE, line_spacing=1.25)
    tag(s, Inches(0.62), y + Inches(2.75), Inches(5.55), "شغّل `DOTNET_TieredCompilation=0` أثناء البروفة لتثبيت العناوين", NAVY2)
    notes(s, "إن سأل المشرف «ليش ما استخدمت bp على الدالة نفسها؟» — أجب: x64dbg لا يعرف رموز .NET (bp Namespace.Class.Method لا يعمل)، "
             "لهذا البحث عن نمط البايتات هو الطريق العملي.")

    # 11 — الكسر 3 + الدائم
    s, y = slide_frame(prs, "10 · CRACK №3 & PERMANENT", "تشريح API… ثم الرتق الدائم",
                       "نفس الهدف بثلاث أدوات — وأمانة التسمية أمام الجمهور")
    box(s, W - Inches(6.3), y + Inches(0.05), Inches(5.8), Inches(3.3), fill=SOFT, line=NAVY, lw=1.2)
    text(s, W - Inches(6.05), y + Inches(0.18), Inches(5.3), Inches(0.4), "تشريح — لا كسر", size=16, bold=True, color=NAVY)
    para_list(s, W - Inches(6.05), y + Inches(0.62), Inches(5.3), Inches(2.5), [
        "أنهِ التجربة: `InstallDate = 20250101`",
        "`bp user32.MessageBoxW` ← **F9** ← صندوق الرسالة",
        "Alt+K: الإطارات `user32` ← `coreclr.dll` ← `clrjit.dll`",
        "Ctrl+F9 ← `EAX = 1` (IDOK) ← عدّله إلى 0",
        "**النتيجة: لا شيء يتغيّر** — لأن `App.xaml.cs` يتجاهل النتيجة",
        "الدرس: القرار قبل الصندوق — لا تُسمِّ تعديل العرض كسراً",
    ], size=13, gap=5)
    box(s, Inches(0.62), y + Inches(0.05), Inches(5.7), Inches(3.3), fill=CODE_BG, line=GOLD, lw=1.2)
    text(s, Inches(0.9), y + Inches(0.18), Inches(5.15), Inches(0.4), "الرتق الدائم — 3 خطوات", size=16, bold=True, color=GOLD)
    code_box(s, Inches(0.9), y + Inches(0.62), Inches(5.15), Inches(2.5), [
        "copy MizanPro.dll MizanPro_cracked.dll",
        "HxD → Ctrl+F → 1F 49 D0 → 49 ← 01 → Save",
        "reset-license.bat",
        "MizanPro.exe → 5NOFK-JQB1Z-7HSHF-NOP6D  ✓",
        "# أو dnSpyEx: Edit IL Body ← 17 2A (ldc.i4.1; ret)",
        "#  ← ثم Save Module (يتطلب PublishSingleFile=false)",
    ], size=12)
    text(s, Inches(0.62), y + Inches(3.5), Inches(12.1), Inches(0.6),
         [("ملاحظة هندسية: ", {"bold": True, "color": RED}),
          ("رتق القرص يبقى سارياً فقط لأن المشروع **غير موقّع (Strong Name)** ولا يفحص تكامله — وهذان أول توصيتين في صفحة الدفاع.", {})], size=15)
    notes(s, "لا تدمج الشريحتين في كلام واحد. قُلها هكذا: «الـ EAX تمرين على فهم المناديات، والرتق على القرص هو الكسر الدائم». "
             "التسمية الدقيقة تُحسب لك في مادة أكاديمية.")

    # 12 — Keygen
    s, y = slide_frame(prs, "11 · KEYGEN", "المفتاح بلا كسر: رياضيات 20 محرفاً",
                       "و«قاعدة الـ 146» التي لم تكتبها ورقة العمل")
    box(s, Inches(0.62), y + Inches(0.05), Inches(5.6), Inches(3.25), fill=SOFT, line=NAVY, lw=1.2)
    text(s, Inches(0.9), y + Inches(0.2), Inches(5.1), Inches(0.4), "الشرطان كما في الكود", size=15.5, bold=True, color=NAVY)
    code_box(s, Inches(0.9), y + Inches(0.65), Inches(5.1), Inches(1.55), [
        "Σ ASCII % 73 == 0",
        "XOR of all chars == 0x0A",
        "# مثال: sum = 1314 = 73 × 18",
    ], size=13)
    para_list(s, Inches(0.9), y + Inches(2.3), Inches(5.05), Inches(1.0), [
        "بتّا XOR مربوطان ⇒ `0x2A` **مستحيلة** (لهذا صارت `0x0A`)",
        "⇒ المجموع زوجي ⇒ اللازم مضاعف **146** لا 73",
    ], size=13, gap=4)
    code_box(s, W - Inches(7.05), y + Inches(0.05), Inches(6.55), Inches(3.25), [
        "def verify(k):",
        "    c = k.replace(\"-\",\"\").upper()",
        "    s, x = sum(map(ord, c)), 0",
        "    for ch in c: x ^= ord(ch)",
        "    return (len(c) == 20",
        "            and s % 73 == 0",
        "            and x == 0x0A)",
        "",
        "def vanity(prefix, tail=5):   # ثبّت 15 وجرّب ذيل",
        "    p = prefix[:15].ljust(15, \"X\")",
        "    while not verify(k := p + rand(tail)):",
        "        pass",
        "    return fmt(k)",
    ], size=11.5, title="keygen.py — مجرَّب: ~4.7 ألف محاولة في المتوسط")
    box(s, Inches(0.62), y + Inches(3.45), Inches(12.1), Inches(0.72), fill=NAVY, radius=True)
    text(s, Inches(0.9), y + Inches(3.56), Inches(11.6), Inches(0.55),
         [("مفاتيح جاهزة: ", {"bold": True, "color": GOLD}),
          ("M1Z4N-2025P-ROM4S-T3R01  ·  MIZAN-2026X-32DBG-B36SQ  ·  CRACK-MEWOR-KSHOP-BY0A5  ·  5NOFK-JQB1Z-7HSHF-NOP6D (بعد الرتق فقط)",
           {"font": MONO_FONT, "size": 13, "color": WHITE})], size=13)
    notes(s, "ورقة العمل تقترح «ثبّت 18 واشتق 2». قِسناها: تنجح في 7% من البادئات فقط (لأن القيدين غير مستقلين). "
             "قل هذا بصوت عالٍ — «عدّلنا تمرين المشرف لأننا جربناه» هو أقوى دليل تعلّم يمكنك عرضه.")

    # 13 — تمرين الطلاب
    s, y = slide_frame(prs, "12 · WORKSHOP", "غداً: أنت تُشرف — لا تُحاضر", "كيف نُدير 3 مجموعات في 40 دقيقة")
    cards = [("١", "قاعدة البداية", "لا تفتح أداة قبل أن تجيب: أين دالة القرار؟ وما الذي ستُثبته؟"),
             ("٢", "توزيع الأدوات", "مجموعة dnSpy (IL)، مجموعة x32dbg (ذاكرة)، مجموعة Procmon (سلوك)"),
             ("٣", "٣ أخطاء نتوقعها", "exe أحادي الملف · بحث في `MizanPro.exe` · `bp Namespace.Method`"),
             ("٤", "سلم التقدير", "٣٠٪ تحليل · ٢٥٪ إثبات سببي · ٢٠٪ أداة بتبرير · ١٥٪ دفاع · ١٠٪ عرض")]
    for i, (n, t, d) in enumerate(cards):
        bx = Inches(0.62) + Inches((i % 2) * 6.22)
        by = y + Inches((i // 2) * 1.55)
        box(s, bx, by, Inches(5.95), Inches(1.38), fill=WHITE, line=RGBColor(0xDE, 0xE6, 0xEF), lw=1.0)
        box(s, bx + Inches(5.55), by, Inches(0.4), Inches(1.38), fill=GOLD)
        text(s, bx + Inches(0.25), by + Inches(0.12), Inches(0.6), Inches(0.5), n,
             size=26, bold=True, color=GOLD, font=MONO_FONT, align="l")
        text(s, bx + Inches(0.95), by + Inches(0.14), Inches(4.5), Inches(0.4), t, size=16.5, bold=True, color=NAVY)
        text(s, bx + Inches(0.95), by + Inches(0.6), Inches(4.45), Inches(0.7), d, size=13.5, color=INK, line_spacing=1.15)
    tag(s, Inches(0.62), y + Inches(3.25), Inches(12.1),
        "أول 10 دقائق مع كل مجموعة: تحقّق من هذه الثلاثة — ثم اتركهم يخبطون رؤوسهم 5 دقائق (إنها الطريقة التي يتعلمون بها)", NAVY2)
    notes(s, "وزّع الأدوار: علي يدير مجموعة x32dbg، ماجد مجموعة dnSpy، المعتصم مجموعة التحليل السلوكي. "
             "لا تُصلح بيدك — وجّه بالسؤال فقط، وسجّل ملاحظاتك لاستخدامها في تقرير «الإشراف».")

    # 14 — ما وجدناه في برنامجنا
    s, y = slide_frame(prs, "13 · SELF-AUDIT", "دقّقنا في برنامجنا قبل أن نُدرّس", "7 لقطات = 4 أعطال حقيقية من جذرين")
    para_list(s, W - Inches(7.0), y + Inches(0.05), Inches(6.55), Inches(3.5), [
        "**جذر ١**: أحداث XAML تعمل أثناء `InitializeComponent` ⇒ `NullReferenceException`",
        "⇒ «حدث خطأ غير متوقع» عند فتح العملاء (لقطة 3)",
        "⇒ `_loading` يعلق ⇒ جداول الفواتير والمنتجات **فاضية** (3 و4)",
        "⇒ «فاتورة جديدة» ينهار — زر لن نضغطه على المسرح",
        "**جذر ٢**: مع `RightToLeft` تُقلب `HorizontalAlignment=\"Left\"` يميناً",
        "⇒ الأزرار والصناديق **فوق العنوان** (لقطات 2، 4، 5)",
        "وأخرى: `Select a date`، لا مشتريات في البذور، وأرصدة لا تتحدث",
    ], size=13.5, gap=5)
    box(s, Inches(0.62), y + Inches(0.05), Inches(5.5), Inches(3.4), fill=SOFT, line=RED, lw=1.5)
    text(s, Inches(0.9), y + Inches(0.2), Inches(5.0), Inches(0.4), "لماذا هذا في عرض كسر؟", size=16, bold=True, color=RED)
    para_list(s, Inches(0.9), y + Inches(0.62), Inches(5.0), Inches(2.7), [
        "الطبقة التي تشرحها يجب أن تُجرَّب — CI كانت خضراء",
        "CI تغطي المحرك (50 فحصاً) ولا تلمس XAML/التخطيط",
        "الدرس للطلاب: «افحص طبقة العرض بيدك، لا بالاختبارات»",
        "كل رقم مُثبت بملف وسطر — `AUDIT_AND_FIXES.md`",
    ], size=12.5, gap=6)
    notes(s, "لا تعتذر — اعرضها كنهج: «قبل أن نُدرّس غيرنا كيف نكسر أنفسنا». هذه الشريحة هي أقوى ما لديك في «التفكير الهندسي»، "
             "وأغلب الظن أنها ستنال أعلى تعليق من المشرف. لا تقرأ النقاط كلها؛ ركّز على الجذر الأول والدرس المستفاد.")

    # 15 — الدفاع
    s, y = slide_frame(prs, "14 · DEFENSE", "كيف نحمي ميزان Pro فعلاً", "كل ثغرة استغللناها ← إصلاح بحجمه")
    rows = [("الثغرة التي استغللناها", "الإصلاح", "الكلفة"),
            ("مفتاح يعمل على أي جهاز", "توقيع ECDSA على `{key|HWID|expiry}` بمفتاح البائع", "متوسطة"),
            ("قرار `bool` في دالة واحدة", "أعِد **مفتاح تشفير وظيفة** بدل `true/false`", "عالية — الأكثر فاعلية"),
            ("حالة قابلة للتحرير في `HKCU`", "DPAPI (`ProtectedData`) + HMAC + عدّاد مضاد للتراجع", "منخفضة"),
            ("AES بمفتاح داخل الحزمة", "لا مفتاح ثابت: اشتقاق من الترخيص أو خادم", "منخفضة"),
            ("`InstallDate` وحده", "`max(last_seen, now)` في مكانين + `GetSystemTimePreciseAsFileTime`", "منخفضة"),
            ("IL نظيف بلا تشويش", "Obfuscation + `PublishAot` (لا IL إطلاقاً ⇒ رتق الفصل 7 يستحيل)", "متوسطة"),
            ("لا فحص تكامل ولا كشف منقِّح", "`SHA256` للحزمة + تجاهل الوظائف الحرجة عند الاختلاف", "منخفضة")]
    ty = y + Inches(0.05)
    for i, r in enumerate(rows):
        h = Inches(0.5 if i else 0.42)
        box(s, Inches(0.62), ty, Inches(12.1), h, fill=(NAVY if i == 0 else (SOFT if i % 2 else WHITE)),
            line=RGBColor(0xE4, 0xEA, 0xF2), lw=0.5)
        text(s, Inches(9.55), ty + Inches(0.07), Inches(3.1), Inches(0.35), r[0],
             size=12.5, bold=(i == 0), color=WHITE if i == 0 else NAVY)
        text(s, Inches(3.35), ty + Inches(0.07), Inches(6.05), Inches(0.35), r[1],
             size=12.5, bold=(i == 0), color=WHITE if i == 0 else INK)
        text(s, Inches(0.75), ty + Inches(0.07), Inches(2.5), Inches(0.35), r[2],
             size=12, bold=(i == 0), color=GOLD if i == 0 else GRAY)
        ty += h + Inches(0.02)
    text(s, Inches(0.62), ty + Inches(0.02), Inches(12.1), Inches(0.62),
         [("الجملة الختامية: ", {"bold": True, "color": NAVY}),
          ("«أقوى حماية تُخرج القرار من الجهاز» — لا طول مفتاح ولا تشفير يحصّنان ترخيصاً محلياً؛ نقترح تفعيل قصير العمر من خادم. ", {}),
          ("تحذير: `PublishAot` يلغي `System.Management` (WMI) في .NET 6 — البصمة تحتاج P/Invoke.", {"color": RED, "bold": True})],
         size=13.5)
    notes(s, "لا تقرأ الجدول. اختر 3 صفوف فقط (التوقيع، مفتاح الوظيفة، فحص التكامل) واربطها بما رأوه قبل دقيقتين. "
             "المقترح النهائي: نسخة v2 بطبقتين فقط — تبقى قابلة للكسر لكنها تُدرّس «لماذا يصعب الأمر».")

    # 16 — خطة الوقت
    s, y = slide_frame(prs, "15 · RUN OF SHOW", "38 دقيقة — دقيقة بدقيقة", "من يتكلم، على أي شاشة، وما الخطة البديلة")
    agenda = [("0–3", "المشكلة والهدف", "علي"), ("3–7", "كيف بُنيت الحماية + الأرقام", "ماجد"),
              ("7–11", "IL/‏JIT + لماذا لا «ملف واحد»", "المعتصم"), ("11–14", "كسر 1: تزوير التخزين", "ماجد"),
              ("14–24", "كسر 2: رتق IL في x32dbg ★", "المعتصم + علي"), ("24–28", "درس التوقيت (JIT)", "علي"),
              ("28–31", "كسر 3: EAX/Call stack = تشريح", "ماجد"), ("31–34", "الرتق الدائم + keygen حيّ", "المعتصم"),
              ("34–37", "الدفاع: ثغرة ← إصلاح", "علي"), ("37–38", "أسئلة + كيف نُشرف", "الثلاثة")]
    ty = y + Inches(0.05)
    for i, (t, w, who) in enumerate(agenda):
        box(s, Inches(0.62), ty, Inches(7.6), Inches(0.42), fill=(NAVY if "★" in w else (SOFT if i % 2 else WHITE)),
            line=RGBColor(0xE4, 0xEA, 0xF2), lw=0.5)
        text(s, Inches(6.55), ty + Inches(0.06), Inches(1.55), Inches(0.3), t, size=13,
             bold=True, color=GOLD if "★" in w else NAVY, font=MONO_FONT, align="l")
        text(s, Inches(2.5), ty + Inches(0.06), Inches(4.0), Inches(0.3), w, size=13.5,
             bold=("★" in w), color=WHITE if "★" in w else INK)
        text(s, Inches(0.75), ty + Inches(0.07), Inches(1.6), Inches(0.3), who, size=12, color=GRAY)
        ty += Inches(0.45)
    box(s, Inches(8.45), y + Inches(0.05), Inches(4.3), Inches(4.5), fill=CODE_BG, line=GOLD, lw=1.2)
    text(s, Inches(8.7), y + Inches(0.2), Inches(3.8), Inches(0.4), "شجرة الطوارئ", size=16, bold=True, color=GOLD)
    para_list(s, Inches(8.7), y + Inches(0.68), Inches(3.85), Inches(3.6), [
        "**A** — كل شيء يعمل: الفصل 7 ← 9 ← 10",
        "**B** — النمط لم يظهر: انتقل لـ HxD فوراً (وقل: أوضح على القرص)",
        "**C** — الأدوات مُعطّلة: اعرض keygen + الفيديو المسجّل + جدول الدفاع",
        "**D** — المنقِّح لا يعمل على جهاز العرض: جهازك المحمول + HDMI",
        "سقوط العرض كله: الشرائح + الـ PDF = محاضرة كاملة بلا ديمو",
        "⏱ لا تتجاوز 38 دقيقة — الإنهاء المبكر أفضل من القطع",
    ], size=12, color=WHITE, gap=6)
    notes(s, "اطبع هذه الشريحة (A5) وضعها أمام من يدير الشاشة. قبل الصعود: `reset-license.bat`، تشغيل التطبيق مرة، "
             "خروج من مجلد x32dbg، إيقاف AV اللحظي، دقة 1920×1080، تكبير x64dbg إلى 12pt.")

    # 16.5 — لقطات العرض (Placeholders)
    s, y = slide_frame(prs, "16 · EVIDENCE", "لقطات الإثبات — ألصقها بعد إعادة التصوير",
                       "ثلاث لقطات تكفي: حالة، رتق، نتيجة")
    shots = [("لوحة التحكم — لافتة «تجربة · ٦ أيام»", "قبل أي كسر: المصدر الوحيد للثقة هو حالة مخزَّنة"),
             ("x32dbg — Patches تعرض `1F 01 D0`", "الدليل البصري على أن الرتق حدث في الذاكرة وحدها"),
             ("شاشة التفعيل — «تم التفعيل بنجاح!»", "نفس المفتاح `5NOFK-…` الذي رُفِض قبل دقيقتين")]
    for i, (t, d) in enumerate(shots):
        bx = Inches(0.62) + Inches(i * 4.2)
        box(s, bx, y + Inches(0.05), Inches(3.95), Inches(2.35), fill=SOFT, line=GOLD, lw=1.25)
        text(s, bx + Inches(0.15), y + Inches(1.0), Inches(3.65), Inches(0.5),
             [("[ هنا تُ لصق اللقطة ]", {"color": GRAY, "size": 13})], align="ctr")
        text(s, bx + Inches(0.15), y + Inches(2.5), Inches(3.65), Inches(0.4), t, size=13.5, bold=True, color=NAVY, align="r")
        text(s, bx + Inches(0.15), y + Inches(2.86), Inches(3.65), Inches(0.5), d, size=12, color=GRAY, align="r")
    tag(s, Inches(0.62), y + Inches(3.35), Inches(12.1),
        "مواعيد التصوير: بعد تنفيذ مهمتَي 1 و2 في `AUDIT_AND_FIXES.md` — وبنفس دقة 1920×1080 حتى لا تتغير القصاصات", NAVY2)
    notes(s, "لا تصوّر قبل الإصلاح: لقطات اليوم ستُظهر الأعطال التي سنذكرها في الشريحة السابقة وستبدو كأنها جزء من العرض. "
             "استخدم Snip & Sketch بمقاس ثابت، وأزل علامة «تنشيط Windows».")

    # 17 — ما تعلّمناه + الختام
    s, y = slide_frame(prs, "16 · TAKEAWAYS", "ما الذي خرجنا به", "٦ نقاط — تُقال في ٦٠ ثانية", dark=True)
    para_list(s, W - Inches(11.4), Inches(3.66), Inches(10.9), Inches(3.2), [
        "**١** كل حماية = فحص + قرار: ابحث عن القرار لا عن «الحماية».",
        "**٢** في .NET: التوقيت (JIT) يصنع الفرق بين كسر يعمل وكسر «يفترض».",
        "**٣** «تشفير بمفتاح داخل الملف» و«حالة في Registry» = لا حماية — أثبتناه في 4 أوامر.",
        "**٤** الأداة لا تُعرّف المهارة: x32dbg للطابق الأصلي، dnSpy للـ IL، Procmon للسلوك.",
        "**٥** الشفافية أكاديمية: سمينَا «EAX» تشريحاً لا كسراً، ووثّقنا أعطال برنامجنا قبل عرضه.",
        "**٦** كل ثغرة نشرحها ← إصلاح نقترحه. هذا الفرق بين هاكر ومهندس.",
    ], size=15.5, color=WHITE, gap=8)
    text(s, W - Inches(8.4), H - Inches(0.75), Inches(7.9), Inches(0.4),
         "شكراً — ونرحّب بالأسئلة (وللمشرف تحديداً: «ما الذي لم نستطع عمله؟» — عندنا إجابة جاهزة)",
         size=14.5, color=GOLD)
    notes(s, "اختم بجملة واحدة عن المستقبل: «نقترح نسخة v2 بطبقتين (توقيع + مفتاح وظيفة) لتكون ورشة الموسم القادم أصعب — "
             "وبذلك يكون مشروعنا قد أنتج هدفين تعليميين لا واحداً». تصفيق + إنهاء.")


def main():
    prs = Presentation()
    prs.slide_width, prs.slide_height = W, H
    build(prs)
    core = prs.core_properties
    core.title = "كسر حماية ميزان Pro بـ x32dbg — ورشة الهندسة العكسية"
    core.author = "علي العطيفي · ماجد الدميني · المعتصم الراعي"
    core.subject = "Project: crack a 3-layer license protection (WPF/.NET 6)"
    core.keywords = "x32dbg, dnSpy, IL patch, keygen, reverse engineering, Arabic RTL"
    core.comments = "كل الأرقام والمفاتيح في هذه الشرائح مُتحقَّق منها؛ التفاصيل في Crack_Guide_x32dbg.pdf"
    prs.save(OUT)
    print("written:", OUT, "slides:", len(prs.slides.__iter__.__self__._sldIdLst))


if __name__ == "__main__":
    main()
