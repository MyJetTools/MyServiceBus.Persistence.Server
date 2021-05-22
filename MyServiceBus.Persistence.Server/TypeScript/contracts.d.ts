
interface ILoadedPage{
    topicId:string;
    writePosition:number;
    messageId:number;
    savedMessageId:number;
    pages:number[];
    activePages:number[];
    queues: ITopicQueue[];
}

interface ITopicQueue{
    queueId:string;
    ranges:IQueueRange[];
}

interface IQueueRange{
    fromId:number;
    toId:number;
}

interface IPersistentOperation{
    name:string;
    topicId:string;
    pageId: number,
    dur:string
}

interface IStatus{
    loadedPages:ILoadedPage[];
    awaitingOperations:IPersistentOperation[];
    queuesSnapshotId:number;
    activeOperations:IPersistentOperation[];
}