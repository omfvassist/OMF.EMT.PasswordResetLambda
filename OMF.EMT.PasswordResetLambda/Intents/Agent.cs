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
    public class Agent : IntentHandler
    {
        #region Generic IntentHandler code

        public Agent(LexV2Event lexEvent, ILambdaContext context, LexV2Response response) : base(lexEvent, context, response, new SlotDefinitions(), new IntentSessionDefinitions())
        {
            Initialize += Agent_Initialize;
            Fulfillment += Agent_Fulfillment;
        }

        public new SlotDefinitions Slots { get { return (SlotDefinitions)base.Slots; } }

        public new IntentSessionDefinitions Session { get { return (IntentSessionDefinitions)base.Session; } }

        #endregion

        #region Define this for each IntentHandler

        public class SlotDefinitions : SlotDefinitionsBase
        {
            public AlphaNumeric TransferToAgent { get; } = new AlphaNumeric();
        }

        public class IntentSessionDefinitions : IntentSessionDefinitionsBase
        {
        }

        #endregion

        private void Agent_Initialize(object sender, EventArgs e)
        {
            if (!Helper.IsWorkingHours())
            {
                AddMessage("We'd be happy to help you during normal business hours M-F 8am-4pm EST. Thank you.");
                //AddMessage(GliaAwsHelper.TextPromptEndEngagement(), ContentType.CustomPayload);
                Response.SessionState.DialogAction.Type = DialogActionType.Close;
            }
        }

        private void Agent_Fulfillment(object sender, EventArgs e)
        {
            Response.SessionState.DialogAction.Type = DialogActionType.Close;

            if (Slots.TransferToAgent.InterpretedValue.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            {
                Helper.TransferToAgent(Response);
            }
            else
            {
                AddMessage("Thanks for chatting with us today, if you need us we're here for you M-F 8am-4pm EST.");
                //AddMessage(GliaAwsHelper.TextPromptEndEngagement(), ContentType.CustomPayload);
            }
        }
    }
}
