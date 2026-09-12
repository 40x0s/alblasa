# ميزان برو — MizanPro 🇸🇦

نظام محاسبة سعودي احترافي — مشروع أكاديمي لمادة **الهندسة العكسية** (السنة الرابعة).

| | |
|---|---|
| اللغة / المنصة | C# — .NET 6 (WPF) |
| الواجهة | عربية RTL بالكامل — **XAML مخصص 100%** بدون أي مكتبات UI جاهزة |
| قاعدة البيانات | SQLite عبر Entity Framework Core 7 |
| البنية | x86 — بدون تحسين (Optimize=false) وبدون PDB (DebugType=none) لتسهيل التحليل |

---

## بنية المشروع

```
MizanPro/
├── MizanPro.csproj              # إعدادات المشروع وحزم NuGet
├── App.xaml / App.xaml.cs       # نقطة الدخول: DB ← Splash ← فحص التفعيل ← دخول ← الرئيسية
│
├── Models/                      # الجزء 2 — النماذج
│   ├── User.cs                  #   المستخدم (مع enum: UserRole)
│   ├── Customer.cs              #   العميل
│   ├── Product.cs               #   المنتج
│   ├── Invoice.cs               #   الفاتورة (مع enum: InvoiceType + InvoiceStatus)
│   └── InvoiceItem.cs           #   صنف الفاتورة
│
├── Data/                        # الجزء 3 — قاعدة البيانات
│   ├── MizanDbContext.cs        #   السياق + الفهارس الفريدة + العلاقات
│   ├── DataSeeder.cs            #   بيانات تجريبية (2 مستخدم + 15 عميلاً + 20 منتجاً + 12 فاتورة)
│   └── Migrations/              #   ملفات EF Migrations (تُولَّد بـ dotnet-ef)
│
├── Core/
│   ├── MizanPaths.cs            #   مسارات %AppData%\MizanPro
│   ├── Engine/                  # الحماية الخفية (محور مادة الهندسة العكسية)
│   │   ├── ProductStateEngine.cs    # Singleton: فحص الحالة + التفعيل (Registry + state.enc)
│   │   └── TokenProcessor.cs        # خوارزمية التحقق من المفتاح + بصمة الجهاز
│   └── Services/                # الجزء 4 — الخدمات (Singleton)
│       ├── SessionManager.cs    #   الجلسة: دخول/خروج/تغيير كلمة المرور (BCrypt)
│       ├── InvoiceService.cs    #   الفواتير: إنشاء + VAT 15% + مخزون + تقارير
│       ├── CustomerService.cs   #   العملاء + الإحصاءات
│       └── ProductService.cs    #   المنتجات + المخزون المنخفض
│
├── Windows/                     # النوافذ (RTL · XAML مخصص بالكامل)
│   ├── SplashWindow.xaml        #   شاشة البداية (BackgroundWorker + مراحل تقدم + تلاشي)
│   ├── AuthWindow.xaml          #   الدخول والتفعيل (اهتزاز عند الفشل + إظهار كلمة المرور)
│   ├── MainWindow.xaml          #   الرئيسية: شريط علوي + قائمة جانبية متحركة (240↔64) + شريط حالة
│   ├── NewInvoiceDialog.xaml    #   نافذة فاتورة جديدة (أصناف قابلة للتحرير + ملخص لحظي)
│   ├── CustomerDialog.xaml      #   نافذة عميل جديد
│   └── ProductDialog.xaml       #   نافذة منتج جديد
│
├── Pages/                       # صفحات MainWindow (تُعرض داخل ContentHost بتلاشي 200ms)
│   ├── DashboardPage.xaml       #   لوحة التحكم: 4 مؤشرات KPI + آخر الفواتير + أبرز العملاء
│   ├── InvoicesPage.xaml        #   الفواتير: فلاتر + شارات حالة + ترقيم صفحات (20/صفحة)
│   ├── CustomersPage.xaml       #   العملاء: عرض بطاقات/قائمة + أفاتار + شريط ديون
│   ├── ProductsPage.xaml        #   المنتجات: بطاقات بأيقونات الفئات + فلاتر المخزون
│   ├── ReportsPage.xaml         #   التقارير: ملخص مالي شهري + توزيع الحالات
│   └── SettingsPage.xaml        #   الإعدادات: 5 تبويبات (شركة/حساب/ترخيص/تفضيلات/عن البرنامج)
│
├── Controls/
│   ├── ActivationPanel.xaml     #   لوحة إدخال مفتاح التفعيل (تنسيق تلقائي)
│   ├── AvatarHelper.cs          #   الأفاتار: لون حتمي + أول حرفين من الاسم
│   └── IRefreshable.cs          #   واجهة تحديث بيانات الصفحات عند التنقل
├── Converters/                  #   محوّلات Enum → نص عربي للعرض
├── Themes/Styles.xaml           #   نظام التصميم: لوحة داكنة/ذهبية + أزرار وحقول وجداول وتبويبات
│
├── build.bat                    # استعادة ← بناء ← نشر ملف واحد (dist\MizanPro.exe)
├── Workshop_Pack.bat            # تجميع حزمة الورشة (Workshop_Delivery\)
├── README_Workshop.md           # دليل ورشة كسر الحماية (للمحاضرة)
├── LabSheet.md                  # ورقة التجارب التفصيلية (6 تجارب + نقاش دفاع)
└── Tools_Links.txt              # روابط الأدوات المطلوبة للورشة
```

## الحزم المستخدمة

| الحزمة | الاستخدام |
|---|---|
| Microsoft.EntityFrameworkCore.Sqlite 7.0.20 | قاعدة البيانات |
| BCrypt.Net-Next 4.0.3 | تجزئة كلمات المرور |
| Newtonsoft.Json 13.0.3 | من حزم المواصفة الأساسية (JSON عام) |
| System.Management 6.0.2 | جلب عنوان MAC (بصمة الجهاز عبر WMI) |

## التشغيل

```bash
dotnet run            # أو افتح المجلد في Visual Studio 2022 واضغط F5
```

- **المتطلبات:** Windows 10/11 + .NET SDK 6.0 أو أحدث
- عند أول تشغيل تُنشأ قاعدة البيانات وتُعبَّأ البيانات التجريبية تلقائياً

### بيانات الدخول التجريبية

| المستخدم | كلمة المرور | الدور |
|---|---|---|
| `admin` | `Admin@123` | مدير النظام |
| `accountant` | `Acc@2024` | محاسب |

## مسارات النظام (خارج المشروع)

| الملف / الموقع | الوصف |
|---|---|
| `%AppData%\MizanPro\mizan.db` | قاعدة البيانات (SQLite) |
| `%AppData%\MizanPro\state.enc` | نسخة الترخيص الاحتياطية المشفرة (AES-128 ← Base64) |
| `HKCU\Software\MizanSolutions\Mizan\RuntimeState` | حالة التشغيل في الـ Registry (ActivationBlob + InstallDate) |

## آلية التجربة والتفعيل (محور الدراسة)

1. **التجربة المجانية:** 7 أيام من أول تشغيل — تُسجَّل قيمة `InstallDate` (بصيغة yyyyMMdd) في الـ Registry.
2. **التفعيل الرئيسي:** قيمة `ActivationBlob` في الـ Registry — إن وُجدت غير صالحة تُحذف تلقائياً (عُدِّلت خارجياً).
3. **المسار الاحتياطي الصامت:** ملف `state.enc` يحتوي `MIZAN_VALID|بصمة الجهاز` مشفّراً بـ AES-128 بمفتاح ثابت مضمّن في التجميعة — يسمح بالتشغيل حتى لو حُذفت قيمة الـ Registry.
4. **بصمة الجهاز** (`TokenProcessor.ComputeMachineFingerprint`): اسم الجهاز + اسم المستخدم + أول عنوان MAC ← SHA256 ← أول 16 خانة hex بصيغة `XXXX-XXXX-XXXX-XXXX` — تربط `state.enc` بجهازه تحديداً.
5. **خوارزمية المفتاح** (`TokenProcessor.ParseAndVerify`): 20 محرفاً من `[A-Z0-9]` يُشترط فيها `مجموع الأكواد % 73 == 0` و `XOR == 0x0A` — التعليق التعليمي الكامل فوق الدالة يتضمن البرهان الرياضي لاستحالة القيمة `0x2A` الواردة في المسودة الأصلية.

### مفاتيح تفعيل صحيحة (للتدريس)

| المفتاح | ملاحظة |
|---|---|
| `M1Z4N-2025P-ROM4S-T3R01` | مفتاح مقروء: المجموع 1314 = 73×18 والـ XOR = 0x0A |
| `AAAAABBBBBCCCCCADN89` | من عائلة مثال المسودة (المثال الأصلي `...DE77A` لا يحقق الشرطين) |

## قواعد العمل المحاسبية

- ضريبة القيمة المضافة **15%** على كل سطر (تُخزَّن `TaxRate=0.15` في كل صنف).
- `SubTotal` = مجموع الأسطر قبل الضريبة، و`Total = SubTotal − Discount + TaxAmount`.
- المبيعات **تنقص** المخزون، والمشتريات والمرتجعات **تزيدانه**.
- إلغاء فاتورة صادرة يعيد الكميات إلى المخزون تلقائياً.
- أرقام الفواتير تسلسلية لكل سنة: `INV-2026-00001`.
- يُمنع حذف عميل أو منتج مرتبط بفواتير (DeleteBehavior.Restrict).

## التكامل المستمر (GitHub Actions)

- `.github/workflows/build.yml` — بناء على Windows + **فحص تشغيلي فعلي (50 فحصاً)** يشغّل
  قاعدة البيانات والخدمات ومحرك الحماية بالكامل: خوارزمية المفاتيح، بصمة الجهاز،
  التجربة والانتهاء، التفعيل، العبث بالـ Registry، ربط `state.enc` بالجهاز،
  ودورة حياة المسودة (لا تحجز المخزون — الإصدار يخصم — الإلغاء يعيد).
- بعدها ينشر سير العمل **الملف التنفيذي الفعلي** (Release · win-x86 · ملف واحد مكتفٍ ذاتياً)
  ويرفعه كـ Artifact باسم `MizanPro-win-x86` يمكن تنزيله من صفحة التشغيل.

## النشر وحزمة الورشة

على جهاز Windows مزوّد بـ .NET SDK 6+:

```bat
build.bat           ← ينتج dist\MizanPro.exe (ملف واحد مكتفٍ ذاتياً)
Workshop_Pack.bat   ← يجمع Workshop_Delivery\:
                       MizanPro.exe + README_Workshop.md + Tools_Links.txt + LabSheet.md
```

- الملف التنفيذي يعمل على Windows 10/11 (32/64-بت) **بدون أي متطلبات إضافية**
  (يحتوي وقت تشغيل .NET والمكتبات الأصلية بما فيها SQLite).
- إعدادات النشر مضمنة في `MizanPro.csproj`: `PublishSingleFile` + `SelfContained` +
  `RuntimeIdentifier=win-x86` — لذا `dotnet publish -c Release` وحده يكفي أيضاً.

## مواد الورشة التعليمية

| الملف | المحتوى |
|---|---|
| `README_Workshop.md` | نظرة عامة: بيانات الدخول، الأدوات، الخطوات المختصرة |
| `LabSheet.md` | 6 تجارب تفصيلية: Procmon ← dnSpy ← Patch ← Keygen ← فك state.enc ← نقاش الدفاع |
| `Tools_Links.txt` | روابط تحميل الأدوات (dnSpy · Procmon · x32dbg · Python) |

## ملاحظات للمطوّر

### الـ Migrations
ملفات `Data/Migrations` **مولَّدة ومُضمَّنة في المستودع** (أُنشئت بأداة `dotnet-ef 7.0.20`)،
و`App.xaml.cs` يطبّقها تلقائياً عند أول تشغيل عبر `Database.MigrateAsync()`.

لتوليد Migration جديدة محلياً:
```bash
dotnet tool install --global dotnet-ef --version 7.0.20
dotnet ef migrations add اسم_الترحيل --output-dir Data/Migrations
```
> **تنبيه:** أداة dotnet-ef تعمل بعملية 64-بت وقد ترفض تحميل تجميعة x86
> ("Could not load assembly"). إن حدث ذلك غيّر `PlatformTarget` مؤقتاً إلى
> `AnyCPU` أثناء التوليد ثم أعده إلى `x86` — محتوى الـ Migration لا يتأثر إطلاقاً
> (هذا ما يفعله سير العمل في GitHub Actions تلقائياً).

### لماذا x86 مع Optimize=false وDebugType=none؟
هذه الإعدادات مقصودة لغرض المادة: إخراج ملف تنفيذي 32-بت بلا تحسين وبلا رموز تصحيح
يجعل تحليله الهندسي العكسي (تفكيك، نقاط توقف، فحص الذاكرة) أوضح وأسهل.
