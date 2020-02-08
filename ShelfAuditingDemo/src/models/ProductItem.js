import uuid from 'uuid';

class ProductItem {
    constructor(detectedObj) {
        let newGuid = uuid.v4();
        
        this.id = newGuid;
        this.displayName = detectedObj.tagName;
        this.model = detectedObj;
    }
}

export { ProductItem };