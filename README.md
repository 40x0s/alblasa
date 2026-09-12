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
│   ├── Licensing/               # محرك التجربة والتفعيل (محور دراسة الهندسة العكسية)
│   │   ├── ProductStateEngine.cs    # فحص الحالة + التفعيل + توليد المفتاح
│   │   ├── ProductState.cs          # enum الحالات + نتيجة الفحص
│   │   └── LicenseRecord.cs         # سجل الترخيص (license.json)
│   └── Services/                # الجزء 4 — الخدمات (Singleton)
│       ├── SessionManager.cs    #   الجلسة: دخول/خروج/تغيير كلمة المرور (BCrypt)
│       ├── InvoiceService.cs    #   الفواتير: إنشاء + VAT 15% + مخزون + تقارير
│       ├── CustomerService.cs   #   العملاء + الإحصاءات
│       └── ProductService.cs    #   المنتجات + المخزون المنخفض
│
├── Windows/                     # نوافذ التشغيل (FlowDirection=RightToLeft)
│   ├── SplashWindow.xaml        #   شاشة البداية (3 ثوانٍ)
│   ├── AuthWindow.xaml          #   تسجيل الدخول (+ استضافة لوحة التفعيل)
│   └── MainWindow.xaml          #   الرئيسية: شريط جانبي + لوحة معلومات + 4 قوائم
│
├── Controls/
│   └── ActivationPanel.xaml     #   لوحة إدخال مفتاح التفعيل
├── Converters/                  #   محوّلات Enum → نص عربي للعرض
└── Themes/Styles.xaml           #   نظام التصميم (ألوان، أزرار، حقول، بطاقات)
```

## الحزم المستخدمة

| الحزمة | الاستخدام |
|---|---|
| Microsoft.EntityFrameworkCore.Sqlite 7.0.20 | قاعدة البيانات |
| BCrypt.Net-Next 4.0.3 | تجزئة كلمات المرور |
| Newtonsoft.Json 13.0.3 | ملف الترخيص |
| System.Management 6.0.2 | جلب عنوان MAC (قفل التفعيل على الجهاز) |

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

| الملف | الوصف |
|---|---|
| `%AppData%\MizanPro\mizan.db` | قاعدة البيانات (SQLite) |
| `%AppData%\MizanPro\license.json` | ملف الترخيص (حالة التجربة/التفعيل) |

## آلية التجربة والتفعيل (محور الدراسة)

1. **التجربة المجانية:** 14 يوماً من أول تشغيل (تُسجَّل في `license.json`).
2. **قفل الجهاز:** معرّف الجهاز = عنوان **MAC** لأول محول شبكة فعّال (عبر WMI).
3. **مفتاح التفعيل:** أول 8 بايتات من `SHA256(معرّف الجهاز + الملح السري)` بصيغة `XXXX-XXXX-XXXX-XXXX`.
4. **قرارات التشغيل** في `App.xaml.cs`:
   - `TrialExpired` → إغلاق التطبيق
   - `TrialActive` / `Licensed` → نافذة الدخول مباشرة
   - `ActivationRequired` (ترخيص غير صالح أو جهاز مختلف) → لوحة التفعيل داخل نافذة الدخول

## قواعد العمل المحاسبية

- ضريبة القيمة المضافة **15%** على كل سطر (تُخزَّن `TaxRate=0.15` في كل صنف).
- `SubTotal` = مجموع الأسطر قبل الضريبة، و`Total = SubTotal − Discount + TaxAmount`.
- المبيعات **تنقص** المخزون، والمشتريات والمرتجعات **تزيدانه**.
- إلغاء فاتورة صادرة يعيد الكميات إلى المخزون تلقائياً.
- أرقام الفواتير تسلسلية لكل سنة: `INV-2026-00001`.
- يُمنع حذف عميل أو منتج مرتبط بفواتير (DeleteBehavior.Restrict).

## التكامل المستمر (GitHub Actions)

- `.github/workflows/build.yml` — بناء على Windows + **فحص تشغيلي فعلي** يشغّل
  قاعدة البيانات والخدمات ومحرك التفعيل ويتحقق من النتائج (23 فحصاً).
- `.github/workflows/generate-migrations.yml` — توليد ملفات EF Migrations عبر `dotnet-ef`.
