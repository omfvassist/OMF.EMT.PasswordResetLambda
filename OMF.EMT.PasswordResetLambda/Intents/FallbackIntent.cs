using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon.Lambda.Core;
using OMF.Amazon.Lambda.LexV2Events;
using OMF.Amazon.Lambda.LexV2FlowControl;

using GliaApiHelper;

namespace OMF.EMT.PasswordResetLambda.Intents
{
    public class FallbackIntent : IntentHandler
    {
        #region Generic IntentHandler code

        public FallbackIntent(LexV2Event lexEvent, ILambdaContext context, LexV2Response response) : base(lexEvent, context, response, new SlotDefinitions(), new IntentSessionDefinitions())
        {
            CodeHook += FallbackIntent_CodeHook;
        }

        public new SlotDefinitions Slots { get { return (SlotDefinitions)base.Slots; } }

        public new IntentSessionDefinitions Session { get { return (IntentSessionDefinitions)base.Session; } }

        #endregion

        #region Define this for each IntentHandler

        public class SlotDefinitions : SlotDefinitionsBase
        {
        }

        public class IntentSessionDefinitions : IntentSessionDefinitionsBase
        {
        }

        #endregion

        private void FallbackIntent_CodeHook(object sender, EventArgs e)
        {
            Response.AddMessage(new LexV2ImageResponseCard
            {
                Title = "Hi, this is OneMain's virtual assistant. I can help reset your password. Is that something you need?",
                Buttons = new[]
                {
                    new LexV2Button { Text = "Yes", Value = "Yes" },
                    new LexV2Button { Text = "No", Value = "No" }
                }
            });

            Response.SessionState.DialogAction.Type = DialogActionType.ElicitIntent;
        }
    }
}
