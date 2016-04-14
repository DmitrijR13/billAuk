using STCLINE.KP50.IFMX.Server.SOURCE.FONTASKS.Unload;
using STCLINE.KP50.Interfaces;


namespace STCLINE.KP50.DataBase
{
    
    /// <summary>
    /// Класс создания задачи
    /// </summary>
    public class MakeFonTask : DataBaseHead
    {
        
        /// <summary>
        /// Получить задачу по ее коду
        /// </summary>
        /// <param name="id"></param>
        /// <param name="calcFonTask"></param>
        /// <returns></returns>
        public BaseFonTask GetFonTask(CalcFonTask calcFonTask)
        {
            BaseFonTask baseTask;
            switch (calcFonTask.TaskType)
            {
                case CalcFonTask.Types.taskKvar:
                    baseTask = new CalcLsFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.UpdatePackStatus:
                    baseTask = new UpdatePackStatusFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.DistributeOneLs:
                    baseTask = new DistribOneLsFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.taskCalcChargeForDelReestr:
                    baseTask = new DeleteReestrFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.uchetOplatArea:
                    baseTask = new UchetOplatForArea(calcFonTask);
                    break;
                case CalcFonTask.Types.taskCalcChargeForReestr:
                    baseTask = new CalcChargeForReestrPerekidok(calcFonTask);
                    break;
                case CalcFonTask.Types.uchetOplatBank:
                    baseTask = new CalcChargeUchetOplatForBank(calcFonTask);
                    break;
                case CalcFonTask.Types.ReCalcKomiss:
                    baseTask = new RecalcUderDistrib(calcFonTask);
                    break;
                case CalcFonTask.Types.taskGetFakturaWeb:
                    baseTask = new FakturaWebTask(calcFonTask);
                    break;
                case CalcFonTask.Types.taskToTransfer:
                    baseTask = new TransferTask(calcFonTask);
                    break;
                case CalcFonTask.Types.taskGeneratePkod:
                    baseTask = new GeneratePkod(calcFonTask);
                    break;
                case CalcFonTask.Types.taskDisassembleFile:
                    baseTask = new DisassembleFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.taskLoadFile:
                    baseTask = new LoadFileFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.taskLoadFileFromSZ:
                    baseTask = new LoadFileFromSzFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.taskLoadFileFromSZpss:
                    baseTask = new LoadFileFromSzFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.CheckBeforeClosingMonth:
                    baseTask = new CheckBeforeClosingMonth(calcFonTask);
                    break;
                case CalcFonTask.Types.taskExportParam:
                    baseTask = new ExportParam(calcFonTask);
                    break;
                case CalcFonTask.Types.taskGenLsPu:
                    baseTask = new GenerateLsPu(calcFonTask);
                    break;
                case CalcFonTask.Types.taskUnloadFileForSZ:
                    baseTask = new UnloadFileForSzFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.taskLoadKladr:
                    baseTask = new LoadKladrFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.taskAutomaticallyChangeOperDay:
                    baseTask = new ChangeOperDayFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.taskChangeOperDay:
                    baseTask = new ManualChangeOperDayFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.taskStartControlPays:
                    baseTask = new StartControlPaysFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.taskRecalcDistribSumOutSaldo:
                    baseTask = new RecalcDistribSumOutSaldo(calcFonTask);
                    break;
                case CalcFonTask.Types.taskUpdateAddress:
                    baseTask = new UpdateAdressFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.taskCalculateAnalytics:
                    baseTask = new CalcAnalizFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.OrderSequences:
                    baseTask = new OrderSequencesFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.AddPrimaryKey:
                    baseTask = new AddPrimaryKeyFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.AddIndexes:
                    baseTask = new AddIndexesFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.AddForeignKey:
                    baseTask = new AddForeignKeyFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.TaskRefreshLSTarif:
                    baseTask = new RefreshLsTarifFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.CancelDistributionAndDeletePack :
                case CalcFonTask.Types.CancelPackDistribution :
                case CalcFonTask.Types.DistributePack :
                    baseTask = new DistribPack(calcFonTask);
                    break;
                case CalcFonTask.Types.taskPreparePrintInvoices:
                    baseTask = new PrintInvoicesFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.taskWithReval:
                    baseTask = new CalcWithRevalFonTask(calcFonTask);
                    break;
                case CalcFonTask.Types.taskWithRevalOntoListHouses:
                    baseTask = new CalcListHousesFonTask(calcFonTask);
                    break;
                default:
                    baseTask = new CalcWithOutRevalFonTask(calcFonTask);
                    break;
            }
            return baseTask;
        




         
        }


    }
}
