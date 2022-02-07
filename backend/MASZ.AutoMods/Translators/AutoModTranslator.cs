﻿using MASZ.Bot.Abstractions;
using MASZ.Bot.Enums;

namespace MASZ.AutoMods.Translators;

public class AutoModTranslator : Translator
{
	public string AutoMod()
	{
		return PreferredLanguage switch
		{
			Language.De => "Auto Mod",
			Language.At => "Auto Mod",
			Language.Fr => "Mode Automatique",
			Language.Es => "Modo Automático",
			Language.Ru => "Авто Мод",
			Language.It => "Mod Automatico",
			_ => "Auto Mod"
		};
	}
}