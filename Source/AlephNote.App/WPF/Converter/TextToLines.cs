﻿using MSHC.WPF.MVVM;
using System.Text.RegularExpressions;

namespace AlephNote.WPF.Converter
{
	class TextToLines : OneWayConverter<string, int>
	{
		public TextToLines() { }

		protected override int Convert(string value, object parameter)
		{
			return Regex.Split(value, @"\r?\n").Length;
		}
	}
}