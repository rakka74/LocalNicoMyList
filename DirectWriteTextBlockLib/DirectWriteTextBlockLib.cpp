// これは メイン DLL ファイルです。

#include "stdafx.h"

#include "DirectWriteTextBlockLib.h"

#define ReleaseInterface(x) { if (NULL != x) { int refCount = x->Release(); /*Debug::WriteLine(refCount);*/ x = NULL; }}

using namespace DirectWriteTextBlockLibNS;
using namespace System::Diagnostics;

DirectWriteTextBlockLib::DirectWriteTextBlockLib()
{
	_text = new std::wstring(L"");
	_fontFamilyName = new std::wstring(L"arial");
	_fontSize = 12;
	_fontWeight = DWRITE_FONT_WEIGHT_REGULAR;

	pin_ptr<IDWriteFactory*> pinDWriteFactory = &_pDWriteFactory;
	DWriteCreateFactory(DWRITE_FACTORY_TYPE_SHARED, __uuidof(IDWriteFactory), (IUnknown**)pinDWriteFactory);

	pin_ptr<ID2D1Factory*> pinD2DFactory = &_pD2DFactory;
	D2D1CreateFactory(D2D1_FACTORY_TYPE_SINGLE_THREADED, (ID2D1Factory**)pinD2DFactory);
}

DirectWriteTextBlockLib::~DirectWriteTextBlockLib()
{
	ReleaseInterface(_pDWriteFactory);
	ReleaseInterface(_pD2DFactory);
	ReleaseInterface(_pRenderTarget);
	ReleaseInterface(_pBrush);

	delete _text;
	delete _fontFamilyName;
}

void MarshalString(String^ s, std::wstring& os) {
	using namespace Runtime::InteropServices;
	const wchar_t* chars =
		(const wchar_t*)(Marshal::StringToHGlobalUni(s)).ToPointer();
	os = chars;
	Marshal::FreeHGlobal(IntPtr((void*)chars));
}

void DirectWriteTextBlockLib::setText(System::String^ text)
{
	if (NULL != _text) {
		delete _text;
	}
	_text = new std::wstring();
	MarshalString(text, *_text);
}

void DirectWriteTextBlockLib::setFontFamilyName(System::String^ fontFamilyName)
{
	if (NULL != _fontFamilyName) {
		delete _fontFamilyName;
	}
	_fontFamilyName = new std::wstring();
	MarshalString(fontFamilyName, *_fontFamilyName);
}

void DirectWriteTextBlockLib::setFontSize(float fontSize)
{
	_fontSize = fontSize;
}

void DirectWriteTextBlockLib::setFontWeight(System::Windows::FontWeight fontWeight)
{
	// TODO
	_fontWeight = DWRITE_FONT_WEIGHT_REGULAR;
}

System::Windows::Size DirectWriteTextBlockLib::getTextSize()
{
	IDWriteTextFormat* pTextFormat;
	_pDWriteFactory->CreateTextFormat(
		_fontFamilyName->c_str(),
		NULL,                       // Font collection (NULL sets it to use the system font collection).
		_fontWeight,
		DWRITE_FONT_STYLE_NORMAL,
		DWRITE_FONT_STRETCH_NORMAL,
		_fontSize,
		L"en-us",
		&pTextFormat
	);
	pTextFormat->SetWordWrapping(DWRITE_WORD_WRAPPING_NO_WRAP);

	IDWriteTextLayout* pTextLayout;
	_pDWriteFactory->CreateTextLayout(_text->c_str(), (UINT32)_text->length(), pTextFormat, 10, 10, &pTextLayout);

	DWRITE_TEXT_METRICS textMetrics;
	pTextLayout->GetMetrics(&textMetrics);

	ReleaseInterface(pTextLayout);
	ReleaseInterface(pTextFormat);

	return System::Windows::Size(textMetrics.width, textMetrics.height);
}

void DirectWriteTextBlockLib::render(IntPtr pResource, bool isNewSurface)
{
	if (isNewSurface) {
		ReleaseInterface(_pBrush);
		ReleaseInterface(_pRenderTarget);

		D2D1_RENDER_TARGET_PROPERTIES rtp;
		ZeroMemory(&rtp, sizeof(rtp));
		rtp.type = D2D1_RENDER_TARGET_TYPE_HARDWARE;
		rtp.pixelFormat = D2D1::PixelFormat(DXGI_FORMAT_UNKNOWN, D2D1_ALPHA_MODE_PREMULTIPLIED);

		IDXGISurface* pDXGISurface = (IDXGISurface*)pResource.ToPointer();

		pin_ptr<ID2D1RenderTarget*> pinRenderTarget = &_pRenderTarget;
		_pD2DFactory->CreateDxgiSurfaceRenderTarget(pDXGISurface, &rtp, pinRenderTarget);

		pin_ptr<ID2D1SolidColorBrush*> pinBrush = &_pBrush;
		_pRenderTarget->CreateSolidColorBrush(D2D1::ColorF(0.0F, 0.0F, 0.0F), pinBrush);
	}

	_pRenderTarget->BeginDraw();

	_pRenderTarget->Clear(D2D1::ColorF(1.0f, 1.0f, 1.0f, 0.0f)); // transparent

	IDWriteTextFormat* pTextFormat;
	_pDWriteFactory->CreateTextFormat(
		_fontFamilyName->c_str(),
		NULL,                       // Font collection (NULL sets it to use the system font collection).
		_fontWeight,
		DWRITE_FONT_STYLE_NORMAL,
		DWRITE_FONT_STRETCH_NORMAL,
		_fontSize,
		L"en-us",
		&pTextFormat
	);
	pTextFormat->SetWordWrapping(DWRITE_WORD_WRAPPING_NO_WRAP);

	IDWriteTextLayout* pTextLayout;
	_pDWriteFactory->CreateTextLayout(_text->c_str(), (UINT32)_text->length(), pTextFormat, 10, 10, &pTextLayout);

	_pRenderTarget->DrawTextLayout(D2D1::Point2F(0, 0), pTextLayout, _pBrush);

	_pRenderTarget->EndDraw();

	ReleaseInterface(pTextLayout);
	ReleaseInterface(pTextFormat);
}
