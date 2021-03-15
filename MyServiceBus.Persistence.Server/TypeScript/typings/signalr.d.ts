/** Represents a connection to a SignalR Hub. */

declare namespace signalR{

    export interface HubConnection{
        connectionState: number;
    }

    export class HubConnectionBuilder{

        withUrl(a:any, o?:any):any;

        invoke(methodName:string, callback:any):Promise<void>;

        on(methodName:string, callback:any);

        start():Promise<void>;

        connection: HubConnection;

    }


}
