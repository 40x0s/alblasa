# docs-builder/ — مولّدات الوثائق

مجلد أدوات البناء فقط (لا شيء هنا يدخل في `MizanPro.csproj` ولا في الحزمة).

## ما الذي يُبنى من هنا
| الملف الناتج في جذر المستودع | المولّد | المحتوى |
|---|---|---|
| `Crack_Guide_x32dbg.pdf` (51 صفحة) | `build_guide_pdf.py` + `guide_part1/2/3.py` | دليل «كسر حماية ميزان Pro بـ x32dbg — من الصفر إلى المية» |
| `MizanPro_Crack_Workshop.pptx` (18 شريحة + ملاحظات المتحدث) | `build_deck_pptx.py` | شرائح عرض الغد |

## التشغيل
```bash
python3 -m pip install --break-system-packages fpdf2 arabic-reshaper python-bidi python-pptx pypdfium2 Pillow
python3 docs-builder/build_guide_pdf.py        # ← يكتب ../Crack_Guide_x32dbg.pdf
python3 docs-builder/build_deck_pptx.py        # ← يكتب ../MizanPro_Crack_Workshop.pptx
python3 docs-builder/_preview.py               # معاينة هندسية للشرائح (PIL) تكشف تجاوز الصناديق
```
الخطوط المطلوبة مرفقة في `fonts/` حتى يكون البناء قابلاً للتكرار على أي جهاز.

## محرّك الطباعة العربية `rtl_pdf.py` — لماذا؟
`fpdf2` لا يطبّق خوارزمية bidi بنفسه بشكل صحيح مع المزيج (عربية + أوامر لاتينية + أرقام) داخل السطر،
و`write_html` يعكس المقاطع اللاتينية. لذلك المحرّك يفعل ثلاث عمليات يدوياً:
1. `arabic_reshaper` ← تشكيل الحروف (Presentation Forms)،
2. تقسيم النص إلى **ذرّات** ثم **عناصر باتجاه واحد**: الكلمة العربية عنصر مستقل، والمقاطع اللاتينية المتتابعة
   تُدمج في كتلة LTR واحدة (فيبقى `bp user32.dll!MessageBoxW` و`1314 = 73` بترتيبهما الصحيح)،
3. حساب العرض بالخط الفعلي لكل مقطع (عربي = DroidNaskh/Kufi، لاتيني = DejaVu/Mono) ثم تلصيق العناصر من اليمين إلى اليسار.

نتيجة ذلك: لا حاجة إلى `RTL_workaround`، والتعليقات العربية تعمل داخل مربّعات الكود الأنجليزية أيضاً.

## اختصارات مهمة للمحرّك
`para · bullets · steps · h1/h2/h3 · new_chapter · cover · code_block · callout(kind=note|warn|danger|ok|step) · table · kv · rule`
وداخل النص: `**عريض**` و`` `كود` `` (مع خلفية خفيفة).

## الخطوط والترخيص (`fonts/LICENSE-fonts.txt`)
- Droid Arabic Naskh / Kufi — Apache License 2.0 (Google / Open Hand).
- DejaVu Sans / Mono — رخصة Bitstream Vera (نصية، مسموحة).
لا توجد خطوط تجارية هنا عمداً؛ إن أردت خطاً أجمل (Cairo / Tajawal / IBM Plex Sans Arabic) ضع ملفاته في `fonts/`
وعدّل أسماء العائلات في أعلى `rtl_pdf.py` — البناء سيعمل كما هو.

## ملاحظتان دقّة
- كل رقم/مسار/اختصار في الـ PDF مطابق للمصدر في هذا المستودع أو لتوثيق x64dbg الرسمي؛ وإن عدّلت الكود
  (مثلاً أعدت تسمية `ActivationBlob`) فحدّث `guide_part2.py` (§5.4) و`guide_part3.py` (ملحق ج) معاً.
- `MizanPro.exe`/`x32dbg` لا يعملان في بيئة البناء (لينكس، بلا .NET ولا واجهة)؛ لذلك كل خطوة في الدليل
  مكتوبة بصيغة «تمرين ثم تأكيد» مع خطة بديلة — ولا تحذف هذه الصياغة.
