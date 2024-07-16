﻿using Reactor.Localization.Utilities;
using System;
using System.Linq;
using MiraAPI.Networking;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MiraAPI.GameOptions.OptionTypes
{
    public class ModdedEnumOption : ModdedOption<int>
    {
        public ModdedEnumOption(string title, int defaultValue, Type enumType, Type roleType) : base(title, defaultValue, roleType)
        {
            Values = Enum.GetNames(enumType);
            Data = ScriptableObject.CreateInstance<StringGameSetting>();
            var data = (StringGameSetting)Data;
            
            data.Title = StringName;
            data.Type = global::OptionTypes.String;
            data.Values = Values.Select(CustomStringName.CreateAndRegister).ToArray();
            data.Index = Value;
        }
        
        public string[] Values { get; set; }

        public override OptionBehaviour CreateOption(ToggleOption toggleOpt, NumberOption numberOpt, StringOption stringOpt, Transform container)
        {
            var stringOption = Object.Instantiate(stringOpt, container);

            stringOption.SetUpFromData(Data, 20);
            stringOption.OnValueChanged = (Il2CppSystem.Action<OptionBehaviour>)ValueChanged;
            
            // SetUpFromData method doesnt work correctly so we must set the values manually
            stringOption.Title = StringName;
            stringOption.Values = ((StringGameSetting)Data).Values;
            stringOption.Value = DefaultValue;

            OptionBehaviour = stringOption;

            return stringOption;
        }
        
        public override NetData GetNetData()
        {
            return new NetData(Id, BitConverter.GetBytes(Value));
        }

        public override void HandleNetData(byte[] data)
        {
            SetValue(BitConverter.ToInt32(data));
        }

        public override string GetHudStringText()
        {
            return Title + ": " + Values[Value];
        }

        public override int GetValueFromOptionBehaviour(OptionBehaviour optionBehaviour)
        {
            return optionBehaviour.GetInt();
        }

        public override void OnValueChanged(int newValue)
        {
            DestroyableSingleton<HudManager>.Instance.Notifier.AddSettingsChangeMessage(StringName, Data.GetValueString(newValue), false);
            if (OptionBehaviour is null) return;

            var opt = OptionBehaviour as StringOption;
            if (opt)
            {
                opt.Value = newValue;
            }
        }
    }
}
