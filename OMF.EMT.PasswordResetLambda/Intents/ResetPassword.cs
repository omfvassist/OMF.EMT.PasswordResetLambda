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
    public class ResetPassword : IntentHandler
    {
        public const string EmailPattern = @"[\w.%+-]+@[\w.-]+\.[a-zA-Z]{2,63}";

        #region Generic IntentHandler code

        public ResetPassword(LexV2Event lexEvent, ILambdaContext context, LexV2Response response) : base(lexEvent, context, response, new SlotDefinitions(), new IntentSessionDefinitions()) {
            Slots.EmailAddress.OriginalValueChanged += EmailAddress_OriginalValueChanged;

            Fulfillment += ResetPassword_Fulfillment;
        }

        public new SlotDefinitions Slots { get { return (SlotDefinitions)base.Slots; } }

        public new IntentSessionDefinitions Session { get { return (IntentSessionDefinitions)base.Session; } }

        #endregion

        #region Define this for each IntentHandler

        public class SlotDefinitions : SlotDefinitionsBase
        {
            // TODO: EmailAddress is an AlphaNumeric rather than a Email type in Lex currently. This is because if you put in an invalid e-mail address it seems to fire the Fallback Intent (you must wait for the bot to be fully ready for testing before it will do this). We need to fix this with flow controls.
            public AlphaNumeric EmailAddress { get; } = new AlphaNumeric();
            public AlphaNumeric TransferToAgent { get; } = new AlphaNumeric();
        }
        
        public class IntentSessionDefinitions : IntentSessionDefinitionsBase
        {
            public IntentSessionItem<int> EmailRetriesRemaining { get; } = 1;
        }

        #endregion

        private void EmailAddress_OriginalValueChanged(object sender, Slot.OriginalValueChangedEventArgs e)
        {
            if (Regex.IsMatch(Slots.EmailAddress.OriginalValue ?? "", EmailPattern))
            {
                Slots.EmailAddress.InterpretedValue = Slots.EmailAddress.OriginalValue;
                Slots.TransferToAgent.InterpretedValue = "No";
            }
            else if (Session.EmailRetriesRemaining.Value-- > 0)
            {
                Slots.EmailAddress.Elicit($"That doesn't look like a valid address. Can you please enter it again?");
            }
            else
            {
                Slots.EmailAddress.InterpretedValue = "";

                AddMessage("I'm sorry, that email address still isn't quite right");

                if (Helper.IsWorkingHours())
                {
                    Slots.TransferToAgent.Elicit(new LexV2ImageResponseCard
                    {
                        Title = "Let's figure this out. Would you like me to see if a Team Member is available?",
                        Buttons = new[]
                        {
                            new LexV2Button { Text = "Yes", Value = "Yes" },
                            new LexV2Button { Text = "No", Value = "No" }
                        }
                    });
                }
                else
                {
                    AddMessage("Unfortunately, we can't transfer you to a Team Member because you've reached us after our regular business hours. Please contact us again M-F 8am-4pm EST. Thank you.");
                    //AddMessage(GliaAwsHelper.TextPromptEndEngagement(), ContentType.CustomPayload);
                    Response.SessionState.DialogAction.Type = DialogActionType.Close;                }
            }
        }

        private void ResetPassword_Fulfillment(object sender, EventArgs e)
        {
            if (Slots.TransferToAgent.InterpretedValue.Equals("Yes", StringComparison.OrdinalIgnoreCase))
            {
                Helper.TransferToAgent(Response);
            }
            else if (Slots.EmailAddress.InterpretedValue != "")
            {
                AddMessage($"Thank you!  We will email password reset instructions to {Slots.EmailAddress.InterpretedValue} from noreply@accountsecurity.omf.com. Remember to check your spam folder.");
                //AddMessage(GliaAwsHelper.TextPromptEndEngagement(), ContentType.CustomPayload);
                SendResetEmail();
            }
            else
            {
                AddMessage("Thanks for chatting with us today, if you need us we're here for you M-F 8am-4pm EST.");
                //AddMessage(GliaAwsHelper.TextPromptEndEngagement(), ContentType.CustomPayload);
            }

            Response.SessionState.DialogAction.Type = DialogActionType.Close;
        }

        private void SendResetEmail()
        {
            try
            {
                var searchBody = MakeHttpRequest($"users?search=profile.email eq \"{Slots.EmailAddress.InterpretedValue}\"", "GET");

                // We didn't find an account for the e-mail.
                if (searchBody == "[]")
                    return;

                searchBody = searchBody.Substring(1, searchBody.Length - 2);        // Strip the [] from the outside of the result.
                var searchJObj = JsonDocument.Parse(searchBody);
                var id = searchJObj.RootElement.GetProperty("id").GetString();

                if (string.IsNullOrEmpty(id))
                    return;

                MakeHttpRequest($"users/{id}/lifecycle/reset_password?sendEmail=true", "POST");
            }
            catch (Exception ex)
            {
                LambdaLogger.Log("SendResetEmail Exception: " + JsonSerializer.Serialize(ex));
            }
        }

        private string MakeHttpRequest(string uri, string method)
        {
            //var baseUri = "https://onemainfinancial-admin.oktapreview.com/api/v1/";

            var baseUri = Environment.GetEnvironmentVariable("OktaBaseUri");
            var authToken = Environment.GetEnvironmentVariable("OktaAuthToken");

            var request = WebRequest.Create(baseUri + uri);
            request.Headers.Add("Authorization", authToken);

            request.Method = method;

            var response = (HttpWebResponse)request.GetResponse();

            using (var responseStream = response.GetResponseStream())
            using (var responseReader = new StreamReader(responseStream))
            {
                var content = responseReader.ReadToEnd();

                if (response.StatusCode != HttpStatusCode.OK)
                    throw new Exception($"{response.StatusCode}:{content}");

                return content;
            }
        }
    }
}
