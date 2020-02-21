import uuid from 'uuid'

export class ProductItem {
    id: string;
    displayName: string;
    model: any;

    constructor(detectedObj: any) {
        let newGuid = uuid.v4();
        
        this.id = newGuid;
        this.displayName = detectedObj.tagName;
        this.model = detectedObj;
    }
}
