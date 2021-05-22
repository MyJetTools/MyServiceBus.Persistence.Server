

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
    
    
    private static renderLoadedPagesContent(topics:ITopicInfo[]):string{
        let result = '';
        
        for (let topic of topics){
            
            let badges = '';
            for (let loadedPage of topic.loadedPages) {
                if (loadedPage.hasSkipped) {
                    badges += '<div><span class="badge badge-danger" style="margin-left: 5px">' + loadedPage.pageId + '</span></div>';
                } else {
                    badges += '<div><span class="badge badge-success" style="margin-left: 5px">' + loadedPage.pageId + '</span></div>';
                }

                badges += '<div>' +
                    '<div class="progress">' +
                    '<div class="progress-bar" role="progressbar" style="width: ' + loadedPage.percent + '%;" aria-valuenow="' + loadedPage.percent + '" aria-valuemin="0" aria-valuemax="100">' + loadedPage.percent + '%</div>'
                    '</div>' +
                   '</div>';
            }

            let activePagesBadges = '';
            for (let activePage of topic.activePages){
                activePagesBadges += '<span class="badge badge-warning" style="margin-left: 5px">'+activePage+'</span>';
            }
            
            let queuesContent = this.renderQueuesTableContent(topic.queues);
            
            
            result += '<tr style="font-size: 12px">' +
                '<td>'+topic.topicId+'<div>WritePos: '+topic.writePosition+'</div>' +
                '<div>Active:</div>'+activePagesBadges+'<hr/><div>Loaded:</div>'+badges+'</td>' +
                '<td>'+queuesContent+'</td>' +
                '<td><div>Current Id:'+topic.messageId+'</div><div>Last Saved:'+topic.savedMessageId+'</div></td>' +
                '</tr>'
        }
        
        return result;
    }
    
    public static renderMainTable(topics:ITopicInfo[]):string {

        let content = this.renderLoadedPagesContent(topics);
        
        return '<table class="table table-striped"><tr><th>Topic</th><th>Queues</th><th>MessageId</th></tr>'+content+'</table>';

    }
    
    private static renderAdditionalFields(r:IStatus):string {
        return '<div>Queue SnapshotId: ' + r.queuesSnapshotId + '</div>';
    }
    
    
    
    private static renderActiveOperations(header:string, activeOperations:IPersistentOperation[]):string{
        
        let result = '<h1>'+header+'</h1><table class="table table-striped"><tr><th>Topic</th><th>Action</th></tr>';
        
        for (let op of activeOperations){
            result +='<tr><td style="font-size:10px">'+op.topicId+'<div>'+op.pageId+'</div></td><td style="font-size:10px">'+op.name+'<div>'+op.dur+'</div></td></tr>';
        }
        
        return result+"</table>";
    }
    
    
    private static splitPage(leftPart, rightPart):string{
     
        return '<table style="width: 100%"><tr>' +
            '<td style="vertical-align: top">'+leftPart+'</td>' +
            '<td style="vertical-align: top">'+rightPart+'</td></tr></table>';
    }
    
    
    public static renderMainContent(r:IStatus):string {
        
        let leftPart = this.renderMainTable(r.topics);
        
        let rightPart = 
            this.renderActiveOperations("Active operations", r.activeOperations)+
            this.renderAdditionalFields(r)+
            this.renderActiveOperations("Awaiting operations", r.awaitingOperations);
        
        return this.splitPage(leftPart, rightPart);
        
        
    }
    
}