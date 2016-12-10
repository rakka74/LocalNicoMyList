// DirectWriteTextBlockLib.h

#pragma once

#include <string>
#include <Dwrite.h>
#include <d2d1.h>

using namespace System;

namespace DirectWriteTextBlockLibNS {

	public ref class DirectWriteTextBlockLib
	{
	public:
		DirectWriteTextBlockLib();
		~DirectWriteTextBlockLib();

		void setText(System::String^ text);
		void setFontFamilyName(System::String^ fontFamilyName);
		void setFontSize(float size);
		void setFontWeight(System::Windows::FontWeight fontWeight);

		System::Windows::Size getTextSize();
		void render(IntPtr, bool);

	private:
		IDWriteFactory* _pDWriteFactory; // 全てのインスタンスで共有したい
		ID2D1Factory* _pD2DFactory; // 全てのインスタンスで共有したい
		ID2D1RenderTarget* _pRenderTarget;
		ID2D1SolidColorBrush* _pBrush;

		std::wstring* _text;
		std::wstring* _fontFamilyName;
		float _fontSize;
		DWRITE_FONT_WEIGHT _fontWeight;
	};
}
