using System.Text;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using System.Text.Json;



namespace DiscordBot
{
    class Program
    {
        DiscordSocketClient client; //봇 클라이언트
        CommandService commands;    //명령어 수신 클라이언트
        /// <summary>
        /// 프로그램의 진입점
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            new Program().BotMain().GetAwaiter().GetResult();   //봇의 진입점 실행
        }

        /// <summary>
        /// 봇의 진입점, 봇의 거의 모든 작업이 비동기로 작동되기 때문에 비동기 함수로 생성해야 함
        /// </summary>
        /// <returns></returns>
        private async Task BotMain()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig() {    //디스코드 봇 초기화
                LogLevel = LogSeverity.Verbose                              //봇의 로그 레벨 설정 
            });
            commands = new CommandService(new CommandServiceConfig()        //명령어 수신 클라이언트 초기화
            {
                LogLevel = LogSeverity.Verbose                              //봇의 로그 레벨 설정
            });

            //로그 수신 시 로그 출력 함수에서 출력되도록 설정
            client.Log += OnClientLogReceived;    
            commands.Log += OnClientLogReceived;

            SECRETS secrets = new SECRETS();
            await client.LoginAsync(TokenType.Bot, secrets.token); //봇의 토큰을 사용해 서버에 로그인
            await client.StartAsync();                         //봇이 이벤트를 수신하기 시작

            client.MessageReceived += OnClientMessage;         //봇이 메시지를 수신할 때 처리하도록 설정

            await Task.Delay(-1);   //봇이 종료되지 않도록 블로킹
        }

        private async Task OnClientMessage(SocketMessage arg)
        {
            //수신한 메시지가 사용자가 보낸 게 아닐 때 취소
            var message = arg as SocketUserMessage;
            if (message == null) return;

            int pos = 0;

            //메시지 앞에 !이 달려있지 않고, 자신이 호출된게 아니거나 다른 봇이 호출했다면 취소
            if (!(message.HasCharPrefix('!', ref pos) ||
             message.HasMentionPrefix(client.CurrentUser, ref pos)) ||
              message.Author.IsBot)
                return;

            if(message.Content != "!spawn")
                return;
            
            var context = new SocketCommandContext(client, message);                    //수신된 메시지에 대한 컨텍스트 생성   

            await context.Channel.SendMessageAsync("Spawned!"); //수신된 명령어를 다시 보낸다.
            client.ButtonExecuted += OnButtonClicked;
            ComponentBuilder componentBuilder = new ComponentBuilder();
            componentBuilder.WithButton("신청하기", "Teacher Register");
            await context.Channel.SendMessageAsync("이 버튼을 눌러서 강좌선생님을 신청하세요!",components:componentBuilder.Build());
        }

        private async Task OnButtonClicked(SocketMessageComponent arg)
        {
            client.ModalSubmitted += OnModalSubmmited;
            ModalBuilder modalBuilder = new ModalBuilder();
            modalBuilder.WithTitle("강좌선생님 신청!");
            modalBuilder.AddTextInput("신청할 강좌 이름(채널명)","channel");
            modalBuilder.AddTextInput("카테고리(프로그래밍, 웹, 해킹, 디자인, 기타)", "category");
            modalBuilder.AddTextInput("강좌에 대한 설명을 작성해주세요", "detail",TextInputStyle.Paragraph);
            modalBuilder.AddTextInput("고정메세지를 잘 읽고 동의하신다면 아레의 입력창에 \"동의합니다\"를 작성해주세요", "agreement");
            modalBuilder.WithCustomId("modal");
            await arg.RespondWithModalAsync(modalBuilder.Build());

        }

        private async Task OnModalSubmmited(SocketModal arg)
        {
            
            List<SocketMessageComponentData> components =
                arg.Data.Components.ToList();

            if (arg.HasResponded == true)
            {
                return;
            }

            if (components.First(x => x.CustomId == "agreement").Value != "동의합니다")
            {
                await arg.RespondAsync("규칙에 동의하지 아니하여서 신청이 취소되었습니다", ephemeral: true);
                return;
            }

            NewUser currentApplication = new NewUser();

            currentApplication.UserName = arg.User.Username;
            currentApplication.RegisteredTime = DateTime.Now;
            currentApplication.ChannelName = components.First(x => x.CustomId == "channel").Value;
            currentApplication.category = components.First(x => x.CustomId == "category").Value;
            currentApplication.detail = components.First(x => x.CustomId == "detail").Value;
            currentApplication.agreement = true;

            string output = $"**{currentApplication.UserName}의 강의신청** \n" +
                            $"• 신청할 강좌 이름 : {currentApplication.ChannelName}\n" +
                            $"• 카테고리 : {currentApplication.category}\n" +
                            $"• 상세설명 : {currentApplication.detail}";

            
           //a
            
            JObject json = new JObject();
            json.Add("UserName",currentApplication.UserName);
            json.Add("RegisteredTime",currentApplication.RegisteredTime);
            json.Add("ChannelName",currentApplication.ChannelName);
            json.Add("category",currentApplication.category);
            json.Add("detail",currentApplication.detail);
            json.Add("agreement",currentApplication.agreement);
            
            Console.WriteLine(json.ToString());

           //  File.Create(         @$"C:\Users\Administrator\Desktop\intergrate with mac\Intergrate-with-server\teaching application\{currentApplication.UserName + "/" + currentApplication.RegisteredTime}.json");
                
           // await File.WriteAllTextAsync(@$"C:\Users\Administrator\Desktop\intergrate with mac\Intergrate-with-server\teaching application\{currentApplication.UserName + "/" + currentApplication.RegisteredTime}.json", json.ToString(),Encoding.Default);
            
           ulong channelID = Convert.ToUInt64(1035826221889626183);
           var textChannel = (SocketTextChannel)client.GetChannel(channelID);

           var message = textChannel.SendMessageAsync(output).Result as IUserMessage;
            
            await arg.RespondAsync("신청이 완료되었습니다",ephemeral:true);
            
            IEmote emote = new Emoji("✅");
            await message.AddReactionAsync(emote);
            
            arg = null;

        }

        /// <summary>
        /// 봇의 로그를 출력하는 함수
        /// </summary>
        /// <param name="msg">봇의 클라이언트에서 수신된 로그</param>
        /// <returns></returns>
        private Task OnClientLogReceived(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());  //로그 출력
            return Task.CompletedTask;
        }
    }

    
    class NewUser
    {
        public string UserName;

        public DateTime RegisteredTime;

        public string ChannelName;

        public string category;

        public string detail;

        public bool agreement;
    }
}
