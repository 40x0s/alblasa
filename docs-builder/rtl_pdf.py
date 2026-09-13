#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
rtl_pdf.py — محرّك طباعة عربي (RTL) فوق fpdf2، مخصّص للوثائق التقنية المختلطة.

لماذا لا نكتفي بـ write_html؟ لأن سطرًا واحداً يمزج العربية مع أوامر المنقِّح
والأرقام يحتاج: (1) تشكيلاً عربياً صحيحاً (Presentation Forms)، (2) تجميعاً
ثنائي الاتجاه يترك المقاطع اللاتينية بتسلسلها الطبيعي، (3) خَطَّين مختلفين في
السطر نفسه، (4) مربّعات كود وجداول ومحاذاة يمنى مع التفاف محسوب الأبعاد.

الخطوط: Droid Arabic Naskh/Kufi (Apache 2.0) + DejaVu (Bitstream Vera).

API مختصر:
    doc.para("نص مع **عريض** و`كود` و1314 رقماً", size=10.6)
    doc.bullets([...]) / doc.steps([...]) / doc.kv([(k,v),...])
    doc.h1/h2/h3 · doc.new_chapter(n, title, sub) · doc.cover(...)
    doc.code_block(text, title="x32dbg", numbers=False)
    doc.callout("warn|note|danger|ok|step", عنوان، [أسطر]) · doc.table(...)
"""
import os
import re

from fpdf import FPDF, Align
import arabic_reshaper
from bidi.algorithm import get_display

HERE = os.path.dirname(os.path.abspath(__file__))
FDIR = os.path.join(HERE, "fonts")

AR, ARB, LAT, MONO = "Arabic", "ArabicB", "Latin", "Mono"

NAVY = (11, 29, 52)
NAVY_SOFT = (30, 56, 88)
INK = (25, 31, 41)
GRAY = (108, 122, 138)
GOLD = (197, 148, 33)
GOLD_DK = (140, 100, 10)
BG = (246, 248, 252)
CODE_BG = (20, 29, 44)
CODE_FG = (222, 233, 248)
CODE_CMT = (124, 198, 158)
CODE_CMD = (255, 208, 120)
GREEN = (18, 120, 70)
RED = (184, 36, 36)
ORANGE = (184, 100, 6)
BLUE = (26, 84, 172)

AR_CHARS = "\u0600-\u06ff\ufb50-\ufeff\u200c-\u200f"
AR_RE = re.compile(f"[{AR_CHARS}]")
SEG_RE = re.compile(f"[{AR_CHARS}]+|[^{AR_CHARS}]+")
KIND_COLOR = {"note": BLUE, "warn": ORANGE, "danger": RED, "ok": GREEN, "step": NAVY}
KIND_LABEL = {"note": "ملاحظة", "warn": "تنبيه", "danger": "خطأ شائع — تجنّبه",
              "ok": "افحص وتأكّد", "step": "خطوة"}


def is_ar(t):
    return bool(AR_RE.search(t))


def visual(t):
    """الشكل الذي يُرسم: عربي ← تشكيل + ترتيب بصري؛ غير ذلك ← كما هو."""
    if not t:
        return ""
    return get_display(arabic_reshaper.reshape(t)) if is_ar(t) else t


def parse_markup(text, base=""):
    """**عريض** و`كود` ← ذرّات: الكلمة العربية بذرها (لتأخذ فراغها)، والمقطع اللاتيني
    كاملاً بمسافاته، ومقطع الكود كذرة واحدة (حتى لا تنقسم الأوامر)."""
    spans, i, style, buf = [], 0, base, ""

    def flush():
        nonlocal buf
        if buf:
            spans.append((style, buf))
            buf = ""

    while i < len(text):
        if text.startswith("**", i):
            j = text.find("**", i + 2)
            flush()
            if j < 0:
                buf += text[i:]
                break
            spans.append(("B", text[i + 2:j]))
            i = j + 2
            continue
        if text[i] == "`":
            j = text.find("`", i + 1)
            if j > i:
                flush()
                spans.append(("code", text[i + 1:j].strip()))
                i = j + 1
                continue
        buf += text[i]
        i += 1
    flush()

    out = []
    for st, chunk in spans:
        if st == "code":
            if chunk:
                out.append((st, chunk))
            continue
        for m in SEG_RE.finditer(chunk):
            seg = m.group(0)
            if is_ar(seg):
                out.extend((st, w) for w in seg.split(" ") if w)
            else:
                seg = re.sub(r"\s+", " ", seg).strip()
                for w_ in seg.split(" "):
                    if w_:
                        out.append((st, w_))
    return out


OPEN = "([{«“‘"


def group_items(atoms):
    """يحوّل الذرّات إلى عناصر (is_latin, segments, gap_before) بالترتيب المنطقي.

    • كل مقطع عربي = عنصر مستقل (بفراغ قبله).
    • المقاطع اللاتينية المتتابعة تُدمج في عنصر LTR واحد داخل فراغات (لتبقى «a = b» بترتيبها).
    • علامات الترقيم وحدها تلصق بالعنصر السابق (بلا فراغ) لأنها تُرسم على يساره في RTL.
    """
    CLOSE = ".,:;!?\u060c\u061b)]}\u201d" + chr(34) + chr(39)
    items = []            # [latin, [(style, seg)], gap_flag]   gap_flag: False = بلا فراغ
    open_pending = False
    for st, seg in atoms:
        latin = not is_ar(seg)
        only_close = latin and seg and all(ch in CLOSE for ch in seg)
        only_open = latin and seg and all(ch in OPEN for ch in seg)
        if only_close and items:
            items[-1][1].append((st, seg))          # تُرسم على يسار العنصر السابق، بلا فراغ
            continue
        if latin and items and items[-1][0] and not only_open:
            prev_code = any(x[0] == "code" for x in items[-1][1])
            if not (prev_code or st == "code") and items[-1][2]:
                items[-1][1].append((st, " "))
                items[-1][1].append((st, seg))
                continue
        gap = not open_pending
        items.append([latin, [(st, seg)], gap, False])
        open_pending = only_open
    return [(lat, ws, 1.0 if gap else 0.0) for lat, ws, gap, _ in items]


class Doc(FPDF):
    def __init__(self, title=""):
        super().__init__(format="A4")
        self.chapter_label = ""
        self.set_margins(18, 17, 18)
        self.set_auto_page_break(False)
        for fam, st, fn in [
            (AR, "", "DroidNaskh-Regular.ttf"), (AR, "B", "DroidNaskh-Bold.ttf"),
            (ARB, "", "DroidKufi-Bold.ttf"), (ARB, "B", "DroidKufi-Bold.ttf"),
            (LAT, "", "DejaVuSans.ttf"), (LAT, "B", "DejaVuSans-Bold.ttf"),
            (MONO, "", "DejaVuSansMono.ttf"), (MONO, "B", "DejaVuSansMono-Bold.ttf"),
        ]:
            p = os.path.join(FDIR, fn)
            if os.path.exists(p):
                self.add_font(fam, st, p)
        self.set_text_color(*INK)

    # ─────────── خطوط ومساحات ───────────
    def face(self, latin, style):
        if style == "code":
            return MONO, ""
        if style == "B":
            return (LAT, "B") if latin else (ARB, "")
        return (LAT, "") if latin else (AR, "")

    def ensure(self, h):
        if self.get_y() + h > self.h - 21:
            self.add_page()
            return True
        return False

    def space_w(self, size):
        """فراغ بين الكلمات: أقصى ما يعطيه الخطّان + حدّ أدنى يضمن الوضوح."""
        self.set_font(AR, "", size)
        a = self.get_string_width(" ")
        self.set_font(LAT, "", size)
        b = self.get_string_width(" ")
        return max(a, b, size * 0.30)

    def seg_w(self, seg, style, size):
        v = visual(seg)
        if not v:
            return 0.0
        f, s = self.face(not is_ar(seg), style)
        self.set_font(f, s, size)
        return self.get_string_width(v)

    def item_w(self, item, size):
        return sum(self.seg_w(t, st, size) for st, t in item[1])

    def item_span(self, item, size, gap):
        return self.item_w(item, size) + gap * item[2]

    # ─────────── رسم ───────────
    def _draw_item(self, x, y, item, size, color, box_pad=0.0):
        """يرسم عنصراً عند x (حدّه الأيسر) ويعيد عرضه — مقاطعه تُرسم يسار→يمين داخل الصندوق."""
        ws = item[1]
        total = sum(self.seg_w(t, st, size) for st, t in ws)
        cx = x
        for st, t in ws:
            v = visual(t)
            if not v:
                continue
            f, s = self.face(not is_ar(t), st)
            self.set_font(f, s, size)
            w = self.get_string_width(v)
            if st == "code":
                self.set_fill_color(235, 239, 245)
                self.rect(cx - 1.0, y + 0.5, w + 2.0, size * 0.30 + 2.6, style="F")
            self.set_text_color(*(GOLD_DK if st == "code" else color))
            self.text(cx, y + size * 0.36, v)
            cx += w
        self.set_text_color(*INK)
        return total


    # ─────────── نصوص أحادية السطر (يمين) مع تبديل الخط تلقائياً ───────────
    def rich_w(self, text, size, bold=False):
        items = group_items(parse_markup(text, "B" if bold else ""))
        gap = self.space_w(size)
        return sum(self.item_w(it, size) + gap * it[2] for it in items)

    def draw_rich(self, x_right, y, text, size, color=INK, bold=False, scale=0.36):
        """يرسم سطراً واحداً حاذياً لليمين عند x_right — بالترتيب العربي الصحيح."""
        items = group_items(parse_markup(text, "B" if bold else ""))
        gap = self.space_w(size)
        x = x_right
        for lat, ws, gf in items:
            x -= self.item_w((lat, ws, gf), size)
            cx = x
            for st, seg in ws:
                v = visual(seg)
                if not v:
                    continue
                f, s = self.face(not is_ar(seg), st)
                self.set_font(f, s, size)
                w = self.get_string_width(v)
                self.set_text_color(*(GOLD_DK if st == "code" else color))
                self.text(cx, y + size * scale, v)
                cx += w
            x -= gap * gf
        self.set_text_color(*INK)
        return x_right - x

    def draw_ltr(self, x_left, y, text, size, color, mono=True):
        """سطر يسار→يمين (لأوامر المنقِّح) مع تشكيل أي تعليق عربي داخله."""
        x = x_left
        for m in SEG_RE.finditer(text):
            seg = m.group(0)
            v = visual(seg)
            if not v:
                continue
            if is_ar(seg):
                self.set_font(AR, "", size * 0.94)
            else:
                self.set_font(MONO if mono else LAT, "", size)
            self.set_text_color(*color)
            w = self.get_string_width(v)
            self.text(x, y + size * 0.36, v)
            x += w
        self.set_text_color(*INK)
        return x - x_left

    def width_of(self, text, size, mono=True):
        return self.draw_ltr(-600, -600, text, size, (0, 0, 0), mono)

    # ─────────── فقرات ───────────
    def para(self, text, size=10.6, lh=None, width=None, right=None, color=INK,
             space_after=2.4, bullet=False, number=None, indent=0.0, bold=False):
        lh = lh or size * 0.635
        width = width if width is not None else (self.w - self.l_margin - self.r_margin - indent)
        right = right if right is not None else (self.w - self.r_margin - indent)
        gap = self.space_w(size)
        atoms = parse_markup(text, "B" if bold else "")

        self.ensure(lh + 1.5)
        if bullet or number is not None:
            y0 = self.get_y()
            mark = "•" if bullet else f"{number}"
            self.set_font(MONO, "B", size * 0.88)
            self.set_text_color(*GOLD)
            mw = self.get_string_width(mark)
            self.text(right, y0 + size * 0.36, mark)
            self.set_text_color(*INK)
            right -= mw + 3.0
            width -= mw + 3.0

        lines = self.wrap_lines(atoms, width, size)
        for line in lines:
            self.ensure(lh)
            y = self.get_y()
            x = right
            for it in line:
                x -= self.item_w(it, size)
                self._draw_item(x, y, it, size, color)
                x -= gap * it[2]
            self.set_y(y + lh)
        self.ln(space_after)

    def bullets(self, items, size=10.2, indent=0.0, space_after=1.6):
        for it in items:
            self.para(it, size=size, bullet=True, indent=indent, space_after=space_after)
        self.ln(1.2)

    def steps(self, items, size=10.2, start=1, indent=0.0):
        for i, it in enumerate(items, start):
            self.para(it, size=size, number=i, indent=indent, space_after=2.0)
        self.ln(1.4)

    def fit(self, text, size, width, bold=False, floor=7.4):
        """يصغّر الخط حتى يسع العنوان في العرض المتاح (بدل أن يخرج من الهامش)."""
        while size > floor and self.rich_w(text, size, bold) > width:
            size -= 0.35
        return size

    # ─────────── عناوين ───────────
    def new_chapter(self, number, title, subtitle=""):
        isnum = isinstance(number, int)
        self.chapter_label = (f"الفصل {number} · {title}" if isnum else title) if number not in (0, None, "") else title
        self.add_page()
        y = self.get_y()
        self.set_fill_color(*NAVY)
        self.rect(18, y, 173, 22, style="F")
        self.set_fill_color(*GOLD)
        self.rect(188.6, y, 2.4, 22, style="F")
        self.set_text_color(255, 255, 255)
        ts = self.fit(title, 16.4, 173 - 30, bold=True)
        self.draw_rich(self.w - self.r_margin - 5, y + 9.0 - ts * 0.36, title, ts, (255,255,255), bold=True, scale=0.36)
        if number not in (0, None, ""):
            self.set_font(MONO, "B", 12 if isnum else 13)
            self.set_text_color(GOLD)
            self.text(24, y + 9.4, f"{number:02d}" if isnum else str(number))
        if subtitle:
            self.set_text_color(192, 210, 232)
            ss = self.fit(subtitle, 9.4, 173 - 30)
            self.draw_rich(self.w - self.r_margin - 5, y + 16.8 - ss * 0.36, subtitle, ss, (192, 210, 232))
        self.set_text_color(*INK)
        self.set_y(y + 27.5)

    def h1(self, text, size=14.4):
        self.ensure(17)
        self.ln(1.4)
        y = self.get_y()
        size = self.fit(text, size, 173, bold=True)
        w = self.rich_w(text, size, bold=True)
        self.set_text_color(*NAVY)
        self.draw_rich(self.w - self.r_margin, y, text, size, NAVY, bold=True)
        self.set_fill_color(*GOLD)
        self.rect(self.w - self.r_margin - max(w, 9), y + size * 0.55, max(w, 9), 1.05, style="F")
        self.set_text_color(*INK)
        self.set_y(y + size * 0.95)
        self.ln(2.6)

    def h2(self, text, size=12.0, color=None):
        self.ensure(12)
        y = self.get_y()
        size = self.fit(text, size, 173 - 6, bold=True)
        self.set_text_color(*(color or NAVY_SOFT))
        self.rect(self.w - self.r_margin - 1.7, y + 1.3, 1.7, size * 0.64, style="F")
        self.draw_rich(self.w - self.r_margin - 4.3, y, text, size, (color or NAVY_SOFT), bold=True)
        self.set_text_color(*INK)
        self.set_y(y + size * 0.82)
        self.ln(1.6)

    def h3(self, text, size=10.8, color=None):
        self.ensure(9)
        y = self.get_y()
        size = self.fit(text, size, 173, bold=True)
        self.set_text_color(*(color or GOLD_DK))
        self.draw_rich(self.w - self.r_margin, y, text, size, (color or GOLD_DK), bold=True)
        self.set_text_color(*INK)
        self.set_y(y + size * 0.76)
        self.ln(1.2)

    # ─────────── صناديق ───────────
    def code_block(self, lines, size=9.0, title="", numbers=False, note=""):
        if isinstance(lines, str):
            lines = [l.rstrip() for l in lines.strip("\n").split("\n")]
        pad, lh = 3.8, size * 0.615
        hh = len(lines) * lh + pad * 2 + (6.6 if title else 0)
        self.ensure(hh + 4)
        y = self.get_y()
        self.set_fill_color(*CODE_BG)
        self.rect(18, y, 173, hh, style="F")
        self.set_fill_color(*GOLD)
        self.rect(18, y, 1.4, hh, style="F")
        yy = y + pad
        if title:
            self.set_text_color(140, 172, 208)
            self.draw_rich(18 + 173 - pad, yy - 1.2, title, size * 0.84, (140, 172, 208), bold=True)
            yy += 6.6
        for i, ln in enumerate(lines, 1):
            t = ln.strip()
            col = CODE_FG
            style = ""
            if t.startswith(("#", "::", "REM", ">", "«")):
                col = CODE_CMT
            elif t.startswith("$"):
                col, style = CODE_CMD, ""
            elif t.startswith("!"):
                col, style = (120, 170, 255), ""
            body = (f"{i:>3}  " if numbers else "") + ln
            self.set_text_color(*col)
            avail = 173 - pad * 2 - (6 if numbers else 0)
            if is_ar(ln) and ln.lstrip().startswith(("#", "REM", "::")):
                self.draw_rich(18 + 173 - pad, yy, ln, size, col)
            else:
                sz = size * min(1.0, avail / max(avail, self.width_of(body, size)))
                self.draw_ltr(18 + pad + (6 if numbers else 0), yy + sz * 0.30, body, sz, col)
            self.set_text_color(*col)
            yy += lh
        self.set_text_color(*INK)
        self.set_y(y + hh + 1.6)
        if note:
            self.para(note, size=8.6, color=GRAY, space_after=2.2)
        self.ln(1.6)

    def callout(self, kind, title, body, color=None, fill=None):
        color = color or KIND_COLOR.get(kind, NAVY)
        label = KIND_LABEL.get(kind, "")
        if isinstance(body, str):
            body = [body]
        inner_w = 173 - 13
        lh = 9.6 * 0.635
        n = 0
        for b in body:
            n += max(1, len(self.wrap_lines(parse_markup(b, ""), inner_w, 9.6)))
        est = 11.0 + n * lh + 2.0
        self.ensure(est)
        y = self.get_y()
        self.set_fill_color(*(fill or BG))
        self.rect(18, y, 173, est, style="F")
        self.set_fill_color(*color)
        self.rect(18 + 173 - 2.2, y, 2.2, est, style="F")
        self.set_text_color(*color)
        ct = f"**{title}**  ·  {label}" if title else label
        cs = self.fit(ct, 10.2, 173 - 14, bold=True)
        self.draw_rich(18 + 173 - 6, y + 6.6 - cs * 0.36, ct, cs, color, bold=True)
        self.set_text_color(*INK)
        self.set_y(y + 10.6)
        for b in body:
            self.para(b, size=9.6, width=inner_w, right=18 + 173 - 7, space_after=1.1)
        self.set_y(y + est + 3.4)

    def wrap_lines(self, atoms, width, size):
        """توزيع الذرّات على أسطر؛ العناصر الأطول من العمود تُقسَر (hard break)."""
        gap = self.space_w(size)
        items = group_items(atoms)
        fitted = []
        for it in items:
            w = self.item_w(it, size)
            if w <= width or not it[1]:
                fitted.append(it)
                continue
            lat, ws, gf = it
            if len(ws) > 1:                      # فرْم بين المقاطع
                for st, seg in ws:
                    if seg.strip():
                        fitted.append((not is_ar(seg), [(st, seg)], 1.0))
                continue
            st, seg = ws[0]                      # فاصل حرفي داخل مقطع واحد
            cur, cw = "", 0.0
            for ch in seg:
                cw += self.seg_w(ch, st, size)
                if cw > width and cur:
                    fitted.append((not is_ar(cur), [(st, cur)], 1.0))
                    cur, cw = ch, self.seg_w(ch, st, size)
                else:
                    cur += ch
            if cur:
                fitted.append((not is_ar(cur), [(st, cur)], 1.0))
        lines, cur, w = [], [], 0.0
        for it in fitted:
            span = self.item_span(it, size, gap)
            if cur and w + span > width:
                lines.append(cur)
                cur, w = [it], span
            else:
                cur.append(it)
                w += span
        if cur:
            lines.append(cur)
        return lines

    # ─────────── جداول ───────────
    def table(self, headers, rows, widths=None, size=9.2, align=None, zebra=True, pad=2.4):
        n = len(headers)
        widths = widths or [173 / n] * n
        sc = 173 / sum(widths)
        widths = [w * sc for w in widths]
        align = align or (["R"] + ["L"] * (n - 1))
        lh = size * 0.62
        gap = self.space_w(size)

        def cell_lines(txt, ci):
            return self.wrap_lines(parse_markup(str(txt), ""), widths[ci] - pad * 2, size)

        self.ensure(9.5)
        y = self.get_y()
        self.set_fill_color(*NAVY)
        self.rect(18, y, 173, 8.4, style="F")
        x = 18 + 173
        for i, hh in enumerate(headers):
            x -= widths[i]
            self.set_text_color(255, 255, 255)
            items = group_items(parse_markup(hh, "B"))
            tot = sum(self.item_w(it, size) for it in items)
            cx = (x + widths[i] - pad - tot) if align[i] == "R" else (x + pad)
            hg = self.space_w(size)
            for ii, it in enumerate(items):
                if ii:
                    cx += hg
                cx += self._draw_item(cx, y + 1.9, it, size, (255, 255, 255))
        self.set_y(y + 8.8)
        self.set_text_color(*INK)

        for ri, row in enumerate(rows):
            cells = [cell_lines(c, i) for i, c in enumerate(row)]
            rh = max(7.0, max(len(c) for c in cells) * lh + 4.2)
            self.ensure(rh)
            y = self.get_y()
            if zebra and ri % 2 == 0:
                self.set_fill_color(249, 251, 254)
                self.rect(18, y, 173, rh, style="F")
            self.set_draw_color(226, 233, 242)
            self.set_line_width(0.2)
            self.line(18, y + rh, 191, y + rh)
            x = 18 + 173
            for ci, lines in enumerate(cells):
                x -= widths[ci]
                if ci:
                    self.line(x, y + 1.2, x, y + rh - 1.2)
                yy = y + 3.2
                for ln in lines:
                    if not ln:
                        yy += lh
                        continue
                    total = sum(self.item_w(it, size) + gap * it[2] for it in ln)
                    if align[ci] == "R":
                        cx = x + widths[ci] - pad
                        for it in ln:
                            cx -= self.item_w(it, size)
                            self._draw_item(cx, yy, it, size, INK)
                            cx -= gap * it[2]
                    else:
                        cx = x + pad
                        for it in ln:
                            cx += self._draw_item(cx, yy, it, size, INK)
                            cx += gap * it[2]
                    yy += lh
            self.set_y(y + rh)
        self.ln(3.0)

    def kv(self, pairs, size=9.8, keyw=54, color=NAVY):
        """سطر «مفتاح : قيمة» — الارتفاع يحسب على عدد أسطر القيمة (بلا انزياح)."""
        lh = size * 0.74
        for k, v in pairs:
            items = group_items(parse_markup(v, ""))
            gap = self.space_w(size)
            avail = (self.w - self.r_margin - keyw) - self.l_margin
            vals = [(st, t) for it in items for st, t in it[1]]
            n = max(1, len(self.wrap_lines(vals, avail, size)))
            row_h = n * lh + 2.6
            self.ensure(row_h)
            y = self.get_y()
            self.set_text_color(*color)
            self.draw_rich(self.w - self.r_margin, y, k, size, color, bold=True)
            self.set_text_color(*INK)
            # رسم أسطر القيمة (يمين→يسار، وكل سطر محاذى لليمين عند عمود القيمة)
            lines = self.wrap_lines(vals, avail, size)
            yy = y
            for ln in lines:
                cx = self.w - self.r_margin - keyw
                for it in ln:
                    cx -= self.item_w(it, size)
                    self._draw_item(cx, yy, it, size, INK)
                    cx -= gap * it[2]
                yy += lh
            self.set_draw_color(224, 232, 241)
            self.set_line_width(0.2)
            self.line(18, y + row_h - 1.0, 191, y + row_h - 1.0)
            self.set_y(y + row_h)
        self.ln(0.8)

    def rule(self, color=(208, 220, 234)):
        self.ensure(5)
        y = self.get_y()
        self.set_draw_color(*color)
        self.set_line_width(0.3)
        self.line(18, y + 1.7, 191, y + 1.7)
        self.set_y(y + 4.4)

    def spacer(self, h=3):
        self.ln(h)

    def footer(self):
        self.set_y(-13.0)
        self.set_draw_color(*GOLD)
        self.set_line_width(0.25)
        self.line(18, self.y - 3.6, 191, self.y - 3.6)
        self.set_font(MONO, "", 7.4)
        self.set_text_color(*GRAY)
        self.cell(0, 5, f"{self.page_no():02d}", align=Align.R)
        self.draw_rich(150, self.y - 2.6, self.chapter_label, 8.2, GRAY)
        self.set_text_color(*INK)

    def cover(self, kicker, title, subtitle, meta, foot_title, foot_lines):
        self.add_page()
        self.set_fill_color(*NAVY)
        self.rect(0, 0, 210, 124, style="F")
        self.set_fill_color(*GOLD)
        self.rect(0, 124, 210, 2.4, style="F")
        self.set_text_color(206, 176, 96)
        self.set_font(MONO, "B", 9)
        self.text(192 - self.get_string_width(kicker), 28, kicker)
        self.set_text_color(255, 255, 255)
        self.draw_rich(192, 54 - 25 * 0.36, title, self.fit(title, 25, 174, bold=True, floor=13), (255, 255, 255), bold=True)
        self.set_text_color(188, 206, 228)
        self.draw_rich(192, 68 - 12.4 * 0.36, subtitle, self.fit(subtitle, 12.4, 174, floor=8.5), (188, 206, 228))
        self.set_fill_color(*GOLD)
        self.rect(150, 76, 42, 1.0, style="F")
        self.set_y(136)
        self.kv(meta, size=10.2, keyw=58)
        self.spacer(3)
        self.callout("danger", foot_title, foot_lines)
