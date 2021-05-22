
interface ITopicInfo{
    topicId:string;
    writePosition:number;
    messageId:number;
    savedMessageId:number;
    loadedPages:ILoadedPage[];
    activePages:number[];
    queues: ITopicQueue[];
}

interface ILoadedPage{
    pageId: number,
    hasSkipped: boolean
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
    topics:ITopicInfo[];
    awaitingOperations:IPersistentOperation[];
    queuesSnapshotId:number;
    activeOperations:IPersistentOperation[];
}