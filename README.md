# ScreenText

برنامج خفيف لـ Windows 10 لاستخراج النصوص الظاهرة على الشاشة ونسخها مباشرة إلى الحافظة.

## الاستخدام

1. شغّل `ScreenText.exe`؛ سيظهر رمز البرنامج بجانب الساعة.
2. اضغط **Ctrl+E**.
3. اسحب بالماوس حول النص المطلوب. اضغط **Esc** للإلغاء.
4. بعد الاستخراج، الصق النص بـ **Ctrl+V**.
5. من قائمة رمز البرنامج، فعّل **التشغيل مع Windows** إذا أردت بدءه تلقائياً.

يعتمد التعرف على Windows OCR وحزم اللغات المثبتة في Windows. ثبّت لغة OCR المناسبة من إعدادات اللغة في Windows للحصول على أفضل نتيجة. يدعم البرنامج اللغات المتاحة في محرك OCR المثبت، بما فيها العربية عند تثبيت حزمة OCR العربية.

## البناء من المصدر

المتطلبات: Windows 10، و.NET 8 SDK.

```powershell
dotnet restore
dotnet publish .\ScreenText.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

الملف الناتج: `bin\Release\net8.0-windows10.0.19041.0\win-x64\publish\ScreenText.exe`.

لا يتطلب البرنامج صلاحيات مسؤول. خيار بدء التشغيل يضيف أو يحذف قيمة `ScreenText` من مفتاح تشغيل المستخدم الحالي في سجل Windows.
