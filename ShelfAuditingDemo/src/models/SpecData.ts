export interface SpecData {
    Id: string;
    Name: string;
    ModelId: string;
    ModelName: string;
    SampleImages: Array<string>;
    CanonicalImages: string;
    ExpectedAreaCoverage: AreaCoverage;
    LowConfidence: number;
    Items: Array<SpecItem>;
}

export interface SpecItem {
    TagId : string;
    TagName: string;  
    ExpectedCount: number;
    ExpectedAreaCoverage: number;
}

export interface AreaCoverage {
    Tagged: number;
    Unknown: number;
    Gap: number;
}
