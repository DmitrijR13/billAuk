using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Activation;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

namespace KP50.DataBase.Migrator.Providers.Validation
{
    /// <summary>
    /// Âñïîìîãàòåëüíûé îáúåêò äëÿ õðàíåíèÿ ïàðàìåòðîâ âûçîâà.
    /// </summary>
    public class ObjectCall
    {
        private readonly RealProxy proxy;
        private readonly IMessage callMessage;
        private readonly Action<IConstructionCallMessage> validationOnInit;
        private readonly Action<IMethodCallMessage> validationOnAction;
        public IMessage ReturnMessage { get; private set; }

        public ObjectCall(
          RealProxy proxy,
          IMessage callMessage,
          Action<IConstructionCallMessage> validationOnInit = null,
          Action<IMethodCallMessage> validationOnAction = null)
        {
            this.proxy = proxy;
            this.callMessage = callMessage;
            this.validationOnInit = validationOnInit;
            this.validationOnAction = validationOnAction;
        }

        /// <summary>
        /// Âûçîâ êîíñòðóêòîðà è èíèöèàëèçàöèÿ
        /// </summary>
        public void Initialize()
        {
            // âàëèäàöèÿ
            if (validationOnInit != null)
            {
                validationOnInit(callMessage as IConstructionCallMessage);
            }

            //  Âûçîâ InitializeServerObject ñîçäàñò 
            //  ýêçåìïëÿð "ñåðâåðíîãî" îáúåêòà
            //  è ñâÿæåò TransparentProxy ñ òåêóùèì êîíòåêñòîì
            ReturnMessage = proxy.InitializeServerObject(
              (IConstructionCallMessage)callMessage);
        }

        /// <summary>
        /// Âûçîâ ìåòîäîâ. 
        /// Ñòîèò îáðàòèòü âíèìàíèå, ÷òî â ìîìåíò âûçîâà ýòîãî ìåòîäà
        /// çíà÷åíèå ñâîéñòâà Context.InternalContextID == __TP.stubData
        ///  Â ïðîòèâíîì ñëó÷àå âûçîâ RemotingServices.ExecuteMessage ïðèâåäåò ê çàöèêëèâàíèþ.
        /// </summary>
        public void Execute()
        {
            // âàëèäàöèÿ
            if (validationOnAction != null)
            {
                validationOnAction(callMessage as IMethodCallMessage);
            }

            ReturnMessage = RemotingServices.ExecuteMessage(
              (MarshalByRefObject)proxy.GetTransparentProxy(),
              (IMethodCallMessage)callMessage);
        }
    }
}
