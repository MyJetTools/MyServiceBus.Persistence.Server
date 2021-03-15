
class Main{
    
    
    private static bodyElement: Element;
    
    private static signalRConnection : signalR.HubConnectionBuilder;

    private static connected = true;
    
    static tickTimer(){

        if (!this.bodyElement)
            this.bodyElement = document.getElementsByTagName("BODY")[0];
        
        if (!this.signalRConnection){
            this.initSignalR();
        }

        if (this.signalRConnection.connection.connectionState != 1){
            this.signalRConnection.start().then(()=>{
                this.connected = true;
            })
                .catch(err => console.error(err.toString()));
        }
    }

    
    private static readDictDifferenceContract<T>(contract) : IDictionaryUpdate<T>{
        return {
            insert: new Dictionary<T>(contract['i']),
            update: new Dictionary<T>(contract['u']),
            delete: contract['d']
        };
    }

    private static initSignalR():void {
        this.signalRConnection = new signalR.HubConnectionBuilder()
            .withUrl("/monitoringhub")
            .build();

        this.signalRConnection.on("init", (data:IInitSignalRContract)=>{
            document.title = data.version;
        });    
        
        
        this.signalRConnection.on("topics", (data:ITopicSignalRContract[])=>{
            this.bodyElement.innerHTML = HtmlRenderer.renderTopics(data);
        });

        this.signalRConnection.on("topics-info", (contract)=>{
            
            let data = new Dictionary<ITopicInfoSignalRContract>(contract);

            data.iterate((topicId, topicInfo)=>{
                let el = document.getElementById(topicId+'-msg-id');
                if (el)
                    el.innerHTML = topicInfo.messageId.toString();

                el = document.getElementById(topicId+'-write-pos');
                if (el)
                    el.innerHTML = topicInfo.writePosition.toString();

                el = document.getElementById(topicId+'-write-queue-size');
                if (el)
                    el.innerHTML = topicInfo.writePosition.toString();
            });

        });
        
    }
    
    
}

window.setInterval(()=>{
    Main.tickTimer();
}, 1000);

$('document').ready(()=>{
    Main.tickTimer();
});