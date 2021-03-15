
interface ILoadedPage{
    topicId:string;
    writePosition:number;
    messageId:number;
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
    id:string;
    name:string;
    topicId:string;
    pageId:number;
    reason:string;
}

interface IStatus{
    loadedPages:ILoadedPage[];
    awaitingOperations:IPersistentOperation[];
    queuesSnapshotId:number;
    activeOperations:IPersistentOperation[];
}

interface IInitSignalRContract{
    version:string;
}

interface ITopicSignalRContract{
    id:string;
}

interface ITopicInfoSignalRContract{
    writePosition:number,
    writeQueueSize:number,
    messageId:number
}


interface IDictionaryUpdate<TValue>{
    insert : Dictionary<TValue>;
    update : Dictionary<TValue>;
    delete : string[];
}
