using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using System.Configuration;
using Newtonsoft.Json.Linq;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Security.AccessControl;
using System.Web.ModelBinding;
using MediaDevices;
using System.Runtime.CompilerServices;
using System.Web.UI.WebControls.Expressions;
using static System.Net.Mime.MediaTypeNames;

namespace ConvSQLCompact
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Program));
        
        static HidroMobileEntities db = new HidroMobileEntities();
        static int contador;
        static int totItens;
        static string caminhoConversorEntrada = Path.Combine("C:\\HidroMobile\\Json\\In");
        static string caminhoConversorSaida = Path.Combine("C:\\HidroMobile\\Json\\Out");
        static bool conectado = false;
        static bool success = false;


        static void Main(string[] args)
        {
            
            if (args.Length == 0)
            {
                Console.WriteLine("Parametro nao fornecido. Devera ser IN (converter arquivos em JSON para SQL) ou OUT (converter arquivos SQL para JSON");
                return;
            }
            if (args[0].ToUpper() == "IN")
            {
                //confere conexão com smartphone
                //caso conectado executa a transferencia de arquivos para o pc
                //se não estiver tudo ok, exibe inicia conversão para sql

                while (conectado == false)
                {
                    VerificarConexao(args);
                }
                if (conectado)
                {
                    while (success == false)
                    {
                        ReceberDoSmartphone(args);
                    }
                    if (success)
                    {
                        success = false;
                        ConverteIn();
                        if (success == false)
                        {
                            Console.WriteLine("\n\nErro na conversão dos dados, o processo será finalizado.");
                            Console.ReadKey();
                        }
                        else
                        {
                            Console.WriteLine("\nArquivos foram convertidos com sucesso.");
                            Environment.Exit(0);
                        }
                    }

                }


            }
            else if (args[0].ToUpper() == "OUT")
            {
                //inicia a conversão para json
                //se estiver tudo ok, inicia transferencia para o smartphone
                while (conectado == false)
                {
                    VerificarConexao(args);
                }
                if (conectado)
                {
                    ConverteOut();
                    if (success)
                    {
                        EnviarParaSmartphone(args);
                    }
                    else
                    {
                        Console.WriteLine("\n\nErro na conversão dos dados, o processo será finalizado.");
                        Console.ReadKey();
                        Environment.Exit(0);
                    }
                }


            }
            else
            {
                Console.WriteLine("Devera ser informado um dos parammetros: IN ou OUT");
            }
        }

        static void VerificarConexao(string[] args)
        {
            var devices = MediaDevice.GetDevices();

            if (devices.ToList().Count <= 0)
            {
                conectado = false;
                Console.WriteLine("Nenhum Smartphone encontrado, verifique a conexão USB e pressione qualquer tecla para tentar novamente...");
                Console.ReadKey();
                Console.Clear();
                return;
            }
            else
            {
                conectado = true;
                return;
            }

        }
        static void EnviarParaSmartphone(string[] args)
        {

            try
            {

                //pega a lista de todos smartphones conectados no pc
                var devices = MediaDevice.GetDevices();

                if (devices.ToList().Count <= 0)
                {
                    success = false;
                    Console.WriteLine("\n\nNenhum Smartphone encontrado, verifique a conexão USB e pressione qualquer tecla para tentar novamente...");
                    Console.ReadKey();
                    Console.Clear();
                    Main(args);
                }
                else
                {
                    //faz a busca pelo smartphone selecionado na view
                    using (var device = devices.First())
                    {

                        //conecta com o smartphone
                        device.Connect();
                        Console.WriteLine("\nConectado com o Smartphone...");


                        //pega o caminho da pasta de armazenamento interno de forma dinâmica
                        var armazenamentoInterno = device.GetDirectories($@"\").First();

                        if (armazenamentoInterno == $@"\")
                        {
                            success = false;
                            Console.WriteLine("\n\nSmartphone mal conenctado, verifique a conexão USB e pressione qualquer tecla para tentar novamente...");
                            Console.ReadKey();
                            Console.Clear();
                            Main(args);
                        }
                        else
                        {
                            Console.WriteLine("Conectado com a pasta interna do Smartphone...");
                            try
                            {
                                Console.WriteLine("Iniciando transferência de arquivos...");

                                if (device.DirectoryExists(Path.Combine(armazenamentoInterno, $@"HidroTemp\In")) == true)
                                {
                                    Console.WriteLine("\n\nFoi detectado um arquvo de rota no smartphone, faça a importação pelo HidroMobile e tente novamente...");
                                    Console.ReadKey();
                                    Console.Clear();
                                    Environment.Exit(0);
                                }

                                //CRIA AS PASTAS NO ANDROID
                                device.CreateDirectory(Path.Combine(armazenamentoInterno, $@"HidroTemp\In"));



                                //PARA CADA ARQUIVO DENTRO DA PASTA OUT DO CONVERSOR
                                var a = Directory.GetFiles(caminhoConversorSaida);
                                foreach (string newPath in Directory.GetFiles(caminhoConversorSaida))
                                {
                                    //PEGA O NOME DO ARQUIVO DE FORMA DINÂMICA
                                    var arquivo = newPath.Split('\\').Last();
                                    Console.WriteLine($"Transferindo arquivo:{arquivo}...");
                                    //SÓ COPIA
                                    device.UploadFile(newPath, $@"{armazenamentoInterno}\HidroTemp\In\{arquivo}");


                                }
                            }
                            catch (Exception ex)
                            {
                                device.Disconnect();
                                success = false;
                                Console.WriteLine("\n\nArquivos não foram copiados, verifique a conexão USB e pressione qualquer tecla para tentar novamente...");
                                Console.ReadKey();
                                Console.Clear();
                                Main(args);
                                return;
                            }
                        }

                        device.Disconnect();
                        success = true;
                        Console.WriteLine("\n\nOs arquivos foram transferidos com êxito para o Smartphone!");
                        System.Environment.Exit(1);
                    }
                }


            }
            catch (Exception ex)
            {
                success = false;
                Console.WriteLine("\n\nArquivos não foram copiados, verifique a conexão USB e pressione qualquer tecla para tentar novamente...");
                Console.ReadKey();
                Console.Clear();
                Main(args);
                return;
            }
            return;

        }

        static void ReceberDoSmartphone(string[] args)
        {
            try
            {
                //pega a lista de todos smartphones conectados no pc
                var devices = MediaDevice.GetDevices();

                if (devices.ToList().Count <= 0)
                {
                    success = false;
                    Console.WriteLine("\n\nNenhum Smartphone encontrado, verifique a conexão USB e pressione qualquer tecla para tentar novamente...");
                    Console.ReadKey();
                    Console.Clear();
                    Main(args);
                }
                else
                {
                    //faz a busca pelo smartphone selecionado na view
                    using (var device = devices.First())
                    {
                        //conecta com o smartphone
                        device.Connect();


                        //pega o caminho da pasta de armazenamento interno de forma dinâmica
                        var armazenamentoInterno = device.GetDirectories($@"\").First();

                        if (armazenamentoInterno == $@"\")
                        {
                            Console.WriteLine("\n\nSmartphone mal conenctado, verifique a conexão USB e pressione qualquer tecla para tentar novamente...");
                            Console.ReadKey();
                            Console.Clear();
                            Main(args);
                        }
                        else
                        {
                            try
                            {
                                //CRIA AS PASTAS NO ANDROID
                                device.CreateDirectory(Path.Combine(armazenamentoInterno, "HidroTemp/Out"));


                                foreach (string newPath in device.GetFiles(Path.Combine(armazenamentoInterno, "HidroTemp/Out")))
                                {
                                    //PEGA O NOME DO ARQUIVO DE FORMA DINÂMICA
                                    var a = newPath;

                                    //var arquivo = newPath.Substring(37);
                                    var arquivo = newPath.Split('\\').Last();

                                    var b = $@"{caminhoConversorEntrada}\{arquivo}";
                                    //VERIFICA SE JÁ EXISTE
                                    if (File.Exists($@"{caminhoConversorEntrada}\{arquivo}"))
                                    {
                                        //DELETA E //COPIA
                                        File.Delete($@"{caminhoConversorEntrada}\{arquivo}");

                                        device.DownloadFile(newPath, $@"{caminhoConversorEntrada}\{arquivo}");

                                    }
                                    else
                                    {
                                        //SÓ COPIA
                                        device.DownloadFile(newPath, $@"{caminhoConversorEntrada}\{arquivo}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                success = false;
                                device.Disconnect();
                                Console.WriteLine("\n\nArquivos não foram copiados, verifique a conexão USB e pressione qualquer tecla para tentar novamente...");
                                Console.ReadKey();
                                Console.Clear();
                                Main(args);
                                var a = ex;
                                return;
                            }
                        }

                        success = true;
                        Console.WriteLine("\n\nOs arquivos foram transferidos com êxito para o Computador!");
                        device.Disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                success = false;
                Console.WriteLine("\n\nArquivos não foram copiados, verifique a conexão USB e pressione qualquer tecla para tentar novamente...");
                Console.ReadKey();
                Console.Clear();
                Main(args);
                var a = ex;
                return;
            }

            return;
        }

        static void ConverteIn()
        {

            Console.WriteLine("\nConvertendo arquivos JSON para Tabelas...");
            contador = 1;
            totItens = 6;

            log.Info($"Iniciando o processo de conversão JSON para tabelas SQL  - {DateTime.Now:r}");
            try
            {
                Console.WriteLine("\nLimpando tabelas...");
                Progresso();

                string pastaIn = ConfigurationManager.AppSettings["FolderIn"];

                log.Info("Realizando a limpeza das tabelas");

                db.Database.ExecuteSqlCommand("DELETE FROM tblCadastro;");
                db.Database.ExecuteSqlCommand("DELETE FROM tblLeitura;");
                db.Database.ExecuteSqlCommand("DELETE FROM tblLogra;");
                db.Database.ExecuteSqlCommand("DELETE FROM tblRota;");

                Console.WriteLine("\nConvertendo tabela tblCadastro..");
                Progresso();

                using (StreamReader r = new StreamReader(pastaIn + "tblCadastro.json"))
                {
                    string json = r.ReadToEnd();
                    var obj = JsonConvert.DeserializeObject<List<tblCadastro>>(json);
                    db.tblCadastro.AddRange(obj);
                }

                Console.WriteLine("\nConvertendo tabela tblLeitura..");
                Progresso();

                using (StreamReader r = new StreamReader(pastaIn + "tblLeitura.json"))
                {
                    string json = r.ReadToEnd();
                    var obj = JsonConvert.DeserializeObject<List<tblLeitura>>(json);
                    Console.WriteLine($"\nTotal de contas: {obj.Count}");
                    db.tblLeitura.AddRange(obj);
                }

                Console.WriteLine("\nConvertendo tabela tblLog..");
                Progresso();

                using (StreamReader r = new StreamReader(pastaIn + "tblLogra.json"))
                {
                    string json = r.ReadToEnd();
                    var obj = JsonConvert.DeserializeObject<List<tblLogra>>(json);
                    db.tblLogra.AddRange(obj);
                }

                Console.WriteLine("\nConvertendo tabela tblOcor.");
                Progresso();

                using (StreamReader r = new StreamReader(pastaIn + "tblRota.json"))
                {
                    string json = r.ReadToEnd();
                    var obj = JsonConvert.DeserializeObject<List<tblRota>>(json);
                    db.tblRota.AddRange(obj);
                }

                Console.WriteLine("\nSalvando os dados no SQL.");
                Progresso();
                success = true;
                db.SaveChanges();

            }
            catch (DbEntityValidationException e)
            {
                var err = "";
                foreach (var eve in e.EntityValidationErrors)
                {
                    foreach (var ve in eve.ValidationErrors)
                    {
                        err += $"propriedade: \"{ve.PropertyName}\", Erro: \"{ve.ErrorMessage}\"";
                    }
                }
                log.Error($"Erro: {err}");
                Console.WriteLine(err);
                success = false;
            }
            catch (System.Data.SqlServerCe.SqlCeException e)
            {
                log.Error($"Ocorreu o seguinte erro: {e.Message}");
                Console.WriteLine(e.Message);
                success = false;
            }
            catch (Exception e)
            {
                log.Error($"Ocorreu o seguinte erro: {e.Message}");
                if (e.InnerException != null)
                {
                    log.Error(e.InnerException.Message);
                    Console.WriteLine($"\n{e.InnerException.InnerException}");
                }
                success = false;
            }
            log.Info($"Termino do processo de conversão JSON para SQL  - {DateTime.Now:r}");
            Console.WriteLine($"\nTermino do processo de conversão JSON para SQL  - {DateTime.Now:r}");

        }
        static void ConverteOut()
        {

            log.Info($"Iniciando o processo de conversão para arquivos no formato JSON  - {DateTime.Now:r}");

            try
            {
                string pasta = ConfigurationManager.AppSettings["FolderOut"];
                totItens = 12;
                contador = 1;

                object[,] tabelas = new object[totItens, 2];

                Console.WriteLine("Carregando tabelas...");

                tabelas[0, 0] = db.tblCadastro.ToList();
                tabelas[0, 1] = "tblCadastro";
                Progresso();

                tabelas[1, 0] = db.tblEncargos.ToList();
                tabelas[1, 1] = "tblEncargos";
                Progresso();

                tabelas[2, 0] = db.tblLeitura.ToList();
                tabelas[2, 1] = "tblLeitura";
                Progresso();

                tabelas[3, 0] = db.tblLog.ToList();
                tabelas[3, 1] = "tblLog";
                Progresso();

                tabelas[4, 0] = db.tblLogra.ToList();
                tabelas[4, 1] = "tblLogra";
                Progresso();

                tabelas[5, 0] = db.tblMensagem.ToList();
                tabelas[5, 1] = "tblMensagem";
                Progresso();

                tabelas[6, 0] = db.tblOcor.ToList();
                tabelas[6, 1] = "tblOcor";
                Progresso();

                tabelas[7, 0] = db.tblPosicao.ToList();
                tabelas[7, 1] = "tblPosicao";
                Progresso();

                tabelas[8, 0] = db.tblRota.ToList();
                tabelas[8, 1] = "tblRota";
                Progresso();

                tabelas[9, 0] = db.tblSistemaAbast.ToList();
                tabelas[9, 1] = "tblSistemaAbast";
                Progresso();

                tabelas[10, 0] = db.tblTarifaTabelas.ToList();
                tabelas[10, 1] = "tblTarifaTabelas";
                Progresso();

                tabelas[11, 0] = db.tblTarifaTabelasCategoria.ToList();
                tabelas[11, 1] = "tblTarifaTabelasCategoria";
                Progresso();
                contador = 1;


                Console.WriteLine("\nConvertendo tabelas em arquivos JSON...");

                for (int i = 0; i < totItens; i++)
                {
                    Progresso();
                    log.Info($"Convertendo tabela {tabelas[i, 1]}");
                    using (StreamWriter file = File.CreateText(@pasta + tabelas[i, 1] + ".json"))
                    {
                        JsonSerializer serializer = new JsonSerializer();
                        serializer.Serialize(file, tabelas[i, 0]);
                    }
                    log.Info($"Tabela {tabelas[i, 1]} convertida com sucesso!");

                }
                success = true;
            }
            catch (Exception e)
            {

                log.Error($"Ocorreu o seguinte erro: {e.Message}");
                success = false;
            }

            log.Info($"Termino do processo de conversão para arquivos no formato JSON  - {DateTime.Now:r}");
        }
        static void Progresso()
        {
            ConsoleUtility.WriteProgressBar(contador++ * 100 / totItens, true);

        }
        static class ConsoleUtility
        {
            const char _block = '■';
            const string _back = "\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b";
            const string _twirl = "-\\|/";
            public static void WriteProgressBar(int percent, bool update = false)
            {
                if (update)
                    Console.Write(_back);
                Console.Write("[");
                var p = (int)((percent / 10f) + .5f);
                for (var i = 0; i < 10; ++i)
                {
                    if (i >= p)
                        Console.Write(' ');
                    else
                        Console.Write(_block);
                }
                Console.Write("] {0,3:##0}%", percent);
            }
            public static void WriteProgress(int progress, bool update = false)
            {
                if (update)
                    Console.Write("\b");
                Console.Write(_twirl[progress % _twirl.Length]);
            }
        }
    }
}
