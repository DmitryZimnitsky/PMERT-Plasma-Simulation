using System;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Linq;

namespace FusionTransformerSimulation
{
    class Program
    {
        const double EvToJoules = 1.602176634e-19;
        const double Mu0 = 4 * Math.PI * 1e-7;

        const double MinorRadius = 0.8;
        const double MajorRadius = 2.4;
        const double MagneticField = 1.25;
        const double WindowAreaFraction = 0.45;
        const double AntennaEfficiency = 0.78;
        const double BaseDensity = 0.8e20;

        struct SimulationResult
        {
            public double Time;
            public double Current;
            public double Beta;
            public double Ti;
            public double PowerIn;
            public double PowerOut;
        }

        static void Main(string[] args)
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            double CrossSectionArea = Math.PI * Math.Pow(MinorRadius, 2);
            double PlasmaLoopLength = 2 * Math.PI * MajorRadius;
            double PlasmaVolume = CrossSectionArea * PlasmaLoopLength;
            double PlasmaInductance = Mu0 * MajorRadius * (Math.Log(8 * MajorRadius / MinorRadius) - 1.75);

            var ThreadResults = new ConcurrentDictionary<int, SimulationResult>();

            Console.WriteLine("Запуск многопоточного симулятора (оптимизация под 12 потоков)...");
            Console.WriteLine("----------------------------------------------------------------------------------------------------");

            Parallel.For(1, 11, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, i =>
            {
                double TargetTime = i * 0.4;

                double n_D = BaseDensity;
                double n_e = BaseDensity;
                double n_T = 0.0;
                double Te = 100.0;
                double Ti = 50.0;
                double PlasmaCurrent = 0.0;

                double t = 0;
                double dt = 5e-9;

                // ОБЪЯВЛЯЕМ ПЕРЕМЕННЫЕ ЗДЕСЬ, ЧТОБЫ ОНИ БЫЛИ ВИДНЫ ЗА ПРЕДЕЛАМИ ЦИКЛА WHILE
                double Beta = 0;
                double P_ohmic = 0;
                double P_fusion_total = 0;
                double P_brem = 0;

                while (t < TargetTime)
                {
                    if (Te < 10.0) Te = 10.0;
                    if (Ti < 10.0) Ti = 10.0;

                    dt = (t > 0.01) ? 2.5e-7 : 5e-9;

                    double PlasmaResistivity = 1.03e-4 * 15.0 / (Te * Math.Sqrt(Te));
                    double PlasmaResistance = PlasmaResistivity * (PlasmaLoopLength / CrossSectionArea);
                    double InductorVoltage = (t < 0.0005) ? 5000.0 : 150.0;

                    double dCurrent = (InductorVoltage - PlasmaCurrent * PlasmaResistance) / PlasmaInductance;
                    PlasmaCurrent += dCurrent * dt;
                    if (PlasmaCurrent < 0) PlasmaCurrent = 0;

                    P_ohmic = (Math.Pow(PlasmaCurrent, 2) * PlasmaResistance) / PlasmaVolume;

                    double P_gas = (n_e * Te + (n_D + n_T) * Ti) * EvToJoules;
                    double P_mag = Math.Pow(MagneticField, 2) / (2 * Mu0);
                    Beta = P_gas / P_mag;

                    double TargetBeta = 0.045;
                    if (Beta > TargetBeta)
                    {
                        double CorrectionFactor = 1.0 - (Beta - TargetBeta) * 20.0;
                        if (CorrectionFactor < 0.05) CorrectionFactor = 0.05;
                        n_D = BaseDensity * CorrectionFactor;
                    }
                    else
                    {
                        n_D = BaseDensity;
                    }
                    n_e = n_D + n_T;

                    double Tau_E = 0.05;
                    if (Beta > 0.05)
                    {
                        Tau_E = 0.05 * Math.Exp(-(Beta - 0.05) * 5);
                        if (Tau_E < 1e-4) Tau_E = 1e-4;
                    }

                    double TiKeV = Ti / 1000.0;
                    double SigmaV_DD = (Ti <= 20000.0) ? 3e-22 * Math.Pow(Ti / 10000.0, 4) : 1e-22;
                    double SigmaV_DT = (TiKeV >= 1.0) ? 3.7e-18 * (TiKeV * TiKeV) / (1.0 + 0.01 * (TiKeV * TiKeV * TiKeV)) : 1e-24;

                    double P_DD_rate = 0.5 * n_D * n_D * SigmaV_DD;
                    double P_DT_rate = n_D * n_T * SigmaV_DT;

                    double dn_T = P_DD_rate - P_DT_rate - (n_T / Tau_E);
                    n_T += dn_T * dt;
                    if (n_T < 0) n_T = 0;

                    double P_DD_charged = P_DD_rate * (2.4e6 * EvToJoules);
                    double P_DT_charged = P_DT_rate * (3.5e6 * EvToJoules);
                    P_fusion_total = P_DD_charged + (P_DT_rate * (17.6e6 * EvToJoules));

                    P_brem = 1.69e-38 * Math.Sqrt(Te) * n_e * n_e;
                    double P_cyc_base = 6.21e-14 * n_e * Math.Pow(MagneticField, 2) * (Te / 1000.0);
                    double P_cyc_recycled = P_cyc_base * (WindowAreaFraction * AntennaEfficiency);
                    double P_cyc_loss_e = P_cyc_base;

                    double TeKeV = Math.Max(Te / 1000.0, 0.01);
                    double P_ei = 5e-40 * (n_e * n_D) * (Te - Ti) / (TeKeV * Math.Sqrt(TeKeV));

                    double P_loss_e = 1.5 * n_e * Te * EvToJoules / Tau_E;
                    double P_loss_i = 1.5 * (n_D + n_T) * Ti * EvToJoules / Tau_E;
                    double P_fuel_cooling = 1.5 * (n_D / 0.05) * (Ti - 10.0) * EvToJoules;

                    double dTe = (P_ohmic - P_ei - P_brem - P_cyc_loss_e - P_loss_e) / (1.5 * n_e * EvToJoules);
                    double dTi = (P_ei + P_DD_charged + P_DT_charged + P_cyc_recycled - P_loss_i - P_fuel_cooling) / (1.5 * (n_D + n_T) * EvToJoules);

                    Te += dTe * dt;
                    Ti += dTi * dt;
                    t += dt;
                }

                // Теперь здесь компилятор успешно прочитает значения
                ThreadResults[i] = new SimulationResult
                {
                    Time = TargetTime,
                    Current = PlasmaCurrent / 1000.0,
                    Beta = Beta * 100.0,
                    Ti = Ti / 1000.0,
                    PowerIn = (P_ohmic * PlasmaVolume) / 1e6,
                    PowerOut = ((P_fusion_total + P_brem) * PlasmaVolume) / 1e6
                };
            });

            Console.WriteLine("Время(с)\tТок(кА)\t\tBeta(%)\t\tT_ионов(кэВ)\tЗатраты_Сеть(МВт)\tПолучено_Тепла(МВт)");
            Console.WriteLine("----------------------------------------------------------------------------------------------------");
            foreach (var key in ThreadResults.Keys.OrderBy(k => k))
            {
                var r = ThreadResults[key];
                Console.WriteLine($"{r.Time:F1}\t\t{r.Current:F1}\t\t{r.Beta:F2}%\t\t{r.Ti:F3}\t\t{r.PowerIn:F2}\t\t\t{r.PowerOut:F2}");
            }

            Console.ReadLine();
        }
    }
}
