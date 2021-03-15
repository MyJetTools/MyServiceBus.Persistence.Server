

class HtmlRenderer
{
    
    
    private static renderQueuesTableContent(queues:ITopicQueue[]):string{
        let result = '';
        
        for (let queue of queues){
            result += '<div><b>'+queue.queueId+'</b></div>';
            
            for (let range of queue.ranges){
                result += '<div style="margin-left: 10px">'+range.fromId+' - '+range.toId+'</div>'; 
            }
            
            result += '<hr/>';
        }
        
        return result;
    }
    
    
    private static renderLoadedPagesContent(pages:ILoadedPage[]):string{
        let result = '';
        
        for (let page of pages){
            
            let badges = '';
            for (let badge of page.pages){
                badges += '<span class="badge badge-success" style="margin-left: 5px">'+badge+'</span>';
            }

            let activePagesBadges = '';
            for (let activePage of page.activePages){
                activePagesBadges += '<span class="badge badge-warning" style="margin-left: 5px">'+activePage+'</span>';
            }
            
            let queuesContent = this.renderQueuesTableContent(page.queues);
            
            result += '<tr><td>'+page.topicId+'<div>WritePos: '+page.writePosition+'</div></td><td>'+queuesContent+'</td><td>'+page.messageId+'</td><td><div>Active:</div>'+activePagesBadges+'<hr/><div>Loaded:</div>'+badges+'</td></tr>'
        }
        
        return result;
    }
    
    public static renderMainTable(pages:ILoadedPage[]):string {

        let content = this.renderLoadedPagesContent(pages);
        
        return '<table class="table table-striped"><tr><th>Topic</th><th>Queues</th><th>MessageId</th><th>Pages</th></tr>'+content+'</table>';

    }
    
    private static renderAdditionalFields(r:IStatus):string {
        return '<div>Queue SnapshotId: ' + r.queuesSnapshotId + '</div>';
    }
    
    
    
    private static renderActiveOperations(header:string, activeOperations:IPersistentOperation[]):string{
        
        let result = '<h1>'+header+'</h1><table class="table table-striped"><tr><th>Name</th><th>Topic</th><th>PageId</th><th>Reason</th></tr>';
        
        for (let op of activeOperations){
            result +='<tr><td>'+op.name+'<div>'+op.id+'</div></td><td>'+op.topicId+'</td><td>'+op.pageId+'</td><td>'+op.reason+'</td></tr>';
        }
        
        return result+"</table>";
    }
    
    
    private static splitPage(leftPart, rightPart):string{
     
        return '<table style="width: 100%"><tr>' +
            '<td style="vertical-align: top">'+leftPart+'</td>' +
            '<td style="vertical-align: top">'+rightPart+'</td></tr></table>';
    }
    
    
    public static renderMainContent(r:IStatus):string {
        
        let leftPart = this.renderMainTable(r.loadedPages);
        
        let rightPart = this.renderActiveOperations("Awaiting operations", r.awaitingOperations) +
            this.renderActiveOperations("Active operations", r.activeOperations)+
            this.renderAdditionalFields(r);
        
        return this.splitPage(leftPart, rightPart);
        
        
    }
    
    
    public static renderTopics(topics:ITopicSignalRContract[]){
        let result = '<table class="table table-striped">' +
            '<tr><th>Topic</th><th>Write pos</th><th>MessageId</th><th>Write queue size</th><th>Queues</th><th>Execution queue</th></tr>';
        
        
        for (let topic of topics){
            result += '<tr>' +
                '<td>'+topic.id+'</td>' +
                '<td id="'+topic.id+'-write-pos"></td>' +
                '<td id="'+topic.id+'-msg-id"></td>' +
                '<td id="'+topic.id+'-write-queue-size"></td>' +
                '<td></td>' +
                '<td></td>' +
                '</tr>';
        }
        
        return  result+'</table>'
        
    }
    
}