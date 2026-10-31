# ScreenText

برنامج خفيف لـ Windows 10 لاستخراج النصوص الظاهرة على الشاشة ونسخها مباشرة إلى الحافظة.

## الاستخدام

1. شغّل `ScreenText.exe`؛ سيظهر رمز البرنامج بجانب الساعة.
2. اضغط **Ctrl+E**.
3. اسحب بالماوس حول النص المطلوب. اضغط **Esc** للإلغاء.
4. بعد الاستخراج، الصق النص بـ **Ctrl+V**.
5. من قائمة رمز البرنامج، فعّل **التشغيل مع Windows** إذا أردت بدءه تلقائياً.

يستخدم البرنامج محرك Tesseract محلياً، مع نماذج OCR العربية والإنجليزية المضمّنة في ملف EXE. لا يحتاج إلى تثبيت حزمة OCR عربية في Windows، ولا يرسل صورة الشاشة إلى الإنترنت. بيانات النماذج تُفك محلياً إلى `%LOCALAPPDATA%\ScreenText\tessdata` عند أول استخدام.

## البناء من المصدر

المتطلبات: Windows 10 و.NET 8 SDK.

```powershell
.\prepare-ocr-data.ps1
dotnet publish .\ScreenText.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o .\publish
```

الملف الناتج: `publish\ScreenText.exe`. يتطلب Tesseract Visual C++ Redistributable 2022 x64 على جهاز التشغيل.

GitHub Actions ينفذ تنزيل النماذج والبناء ويرفع `ScreenText.exe` مع إشعار تراخيص المكونات ضمن Artifact اسمه `ScreenText-win-x64`.

لا يتطلب البرنامج صلاحيات مسؤول. خيار بدء التشغيل يضيف أو يحذف قيمة `ScreenText` من مفتاح تشغيل المستخدم الحالي في سجل Windows.
