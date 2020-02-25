import uuid from 'uuid'

export class ProductItem {
    id: string;
    displayName: string;
    model: PredictionModel;

    constructor(detectedObj: PredictionModel) {
        let newGuid = uuid.v4();
        
        this.id = newGuid;
        this.displayName = detectedObj.tagName;
        this.model = detectedObj;
    }
}

export interface PredictionModel {
    tagId: string;
    tagName: string;
    boundingBox: BoundingBox;
    probability: number;
}

export class BoundingBox {
    left: number;
    top: number;
    height: number;
    width: number;

    constructor(left: number, top: number, width: number, height: number) {
        this.left = left;
        this.top = top;
        this.width = width;
        this.height = height;
    }
}

export class ClassifyImageResponse {
    id: string;
    project: string;
    iteration: string;
    created: string;
    predictions: Array<PredictionModel>;

    constructor() {
        this.id = '';
        this.project = '';
        this.iteration = '';
        this.created = '';
        this.predictions = new Array<PredictionModel>();
    }
}
    