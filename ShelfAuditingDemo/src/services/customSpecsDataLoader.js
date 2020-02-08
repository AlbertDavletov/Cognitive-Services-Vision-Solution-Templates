import localdata from '../assets/specsData.json';

export default class CustomSpecsDataLoader {
    constructor() {
        this.specsDataFileName = "../assets/specsData.json";
        this.specsDataUrl = "https://intelligentkioskstore.blob.core.windows.net/shelf-auditing/specsData.json";
    }

    async getData() {
        return fetch(this.specsDataUrl)
            .then((response) => response.json())
            .then((responseJson) => {
                return responseJson;
            })
            .catch((error) => {
                console.error(error);
                return localdata;
            });
    }

    getLocalData() {
        return localdata;
    }
}