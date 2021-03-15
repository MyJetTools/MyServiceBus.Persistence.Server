
interface IKeyValuePair<TValue> {
    key: string;
    value: TValue;
}

class Dictionary<TValue> {

    private items: object;
    
    constructor(initItems?:any) {
        if (initItems)
            this.items = initItems;
        else
            this.items = {};
    }
    
    
    public init(data):void{
        this.items = data; 
    }

    public add(key: string, value: TValue): void {
        this.items[key] = value;
    }


    public addRange(values: TValue[], getKey: (value: TValue) => string): void {
        for (let i = 0; i < values.length; i++) {
            let value = values[i];
            let key = getKey(value);
            this.add(key, value);
        }
    }

    public remove(key: string): void {
        this.items[key] = undefined;
    }

    public clear(): void {
        this.items = {};
    }

    public getValue(key: string): TValue {
        return this.items[key];
    }

    public getValueOrDefault(key: string, getDefault: () => TValue): TValue {
        let result = this.items[key];

        if (result)
            return result;

        return getDefault();
    }


    public hasKey(key: string): boolean {
        return this.items[key] != undefined;

    }

    public iterate(callbask: (key: string, value: TValue) => void): void {
        let keys = Object.keys(this.items);
        for (let i = 0; i < keys.length; i++) {
            let key = keys[i];
            callbask(key, this.items[key]);
        }
    }

    public getValues(): TValue[] {
        let result: TValue[] = [];

        this.iterateValues(value => {
            result.push(value);
        })

        return result;
    }

    public iterateValues(callbask: (value: TValue) => void): void {
        let keys = Object.keys(this.items);
        for (let i = 0; i < keys.length; i++) {
            let key = keys[i];
            callbask(this.items[key]);
        }
    }

    public getKeys(): string[] {
        return Object.keys(this.items);
    }

    public getCount(): number {
        return this.getKeys().length;
    }

}