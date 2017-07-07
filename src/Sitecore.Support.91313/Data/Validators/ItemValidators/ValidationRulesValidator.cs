using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Validators;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.Save;
using Sitecore.Reflection;
using Sitecore.Rules;
using Sitecore.Rules.Validators;
using Sitecore.SecurityModel;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Shell.Applications.ContentManager;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using static Sitecore.Pipelines.Save.SaveArgs;

namespace Sitecore.Support.Data.Validators.ItemValidators
{
    [Serializable]
    public class ValidationRulesValidator : StandardValidator
    {
        public ValidationRulesValidator()
        {
        }

        public ValidationRulesValidator(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        protected override ValidatorResult Evaluate()
        {
            Item item2;
            Item item = this.GetItem();

            try
            {
                Hashtable hashtable = Context.ClientPage.ServerProperties["Info"] as Hashtable;

                if (hashtable != null && hashtable.Count > 0)
                {
                    Packet savePacket = GetSavePacket(hashtable, item.ID);
                    SaveArgs saveArgs = new SaveArgs(savePacket.XmlDocument);

                    ParseXml parseXml = new ParseXml();
                    parseXml.Process(saveArgs);

                    if (saveArgs.Items.Length > 0)
                    {
                        foreach (Field field in item.Fields)
                        {
                            SaveField[] saveFields = saveArgs.Items[0].Fields.Where(s => s.ID == field.ID).ToArray();
                            if (saveFields != null && saveFields.Count() == 1)
                                field.Value = saveFields[0].Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Sitecore.Support.91313: " + ex.Message, this);
            }

            if (item == null)
            {
                return ValidatorResult.Valid;
            }
            using (new SecurityDisabler())
            {
                item2 = item.Database.GetItem(RuleIds.ValidationRules);
            }
            if (item2 == null)
            {
                return ValidatorResult.Valid;
            }
            ValidatorsRuleContext ruleContext = new ValidatorsRuleContext
            {
                Item = item,
                Validator = this,
                Result = ValidatorResult.Valid,
                Text = string.Empty
            };
            RuleList<ValidatorsRuleContext> rules = RuleFactory.GetRules<ValidatorsRuleContext>(item2, "Rule");
            if (rules == null)
            {
                return ValidatorResult.Valid;
            }
            rules.Run(ruleContext);
            if (ruleContext.Result == ValidatorResult.Valid)
            {
                return ValidatorResult.Valid;
            }
            base.Text = base.GetText(ruleContext.Text, new string[] { item.DisplayName });
            return base.GetFailedResult(ruleContext.Result);
        }
        private static Packet GetSavePacket(Hashtable fieldInfo, ID currentItemID)
        {
            Assert.ArgumentNotNull(fieldInfo, "fieldInfo");
            Packet packet = new Packet();
            foreach (FieldInfo fieldInfo2 in fieldInfo.Values)
            {
                if (!(fieldInfo2.ItemID != currentItemID))
                {
                    System.Web.UI.Control control = Context.ClientPage.FindSubControl(fieldInfo2.ID);
                    if (control != null)
                    {
                        string text;
                        if (control is IContentField)
                        {
                            text = StringUtil.GetString(new string[]
                            {
                        (control as IContentField).GetValue()
                            });
                        }
                        else
                        {
                            text = StringUtil.GetString(ReflectionUtil.GetProperty(control, "Value"));
                        }
                        if (!(text == "__#!$No value$!#__"))
                        {
                            packet.StartElement("field");
                            packet.SetAttribute("itemid", fieldInfo2.ItemID.ToString());
                            packet.SetAttribute("language", fieldInfo2.Language.ToString());
                            packet.SetAttribute("version", fieldInfo2.Version.ToString());
                            packet.SetAttribute("itemrevision", fieldInfo2.Revision.ToString());
                            string a = fieldInfo2.Type.ToLowerInvariant();
                            if (a == "rich text" || a == "html")
                            {
                                text = text.TrimEnd(new char[]
                                {
                            ' '
                                });
                            }
                            packet.SetAttribute("fieldid", fieldInfo2.FieldID.ToString());
                            packet.AddElement("value", text, new string[0]);
                            packet.EndElement();
                        }
                    }
                }
            }
            return Assert.ResultNotNull<Packet>(packet);
        }

        protected override ValidatorResult GetMaxValidatorResult() =>
            base.GetFailedResult(ValidatorResult.Error);

        public override string Name =>
            "Validation rules";
    }
}