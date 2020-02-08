import RNFetchBlob from 'rn-fetch-blob';

export default class CustomVisionService {
    constructor() {
        this.BaseURL = '<ENDPOINT>';
        this.ApiKey = '<KEY>';
        this.MaxRegionsInBatch = 64;
        this.RetryCountOnQuotaLimitError = 6;
        this.RetryDelayOnQuotaLimitError = 500;
    }

    async getIterationsAsync(projectId) {
        const url = this.BaseURL + 'training/projects/' + projectId + '/iterations';
        return fetch(url,
            {
                method: 'GET',
                headers: {
                  'Training-Key': '',
                  'Training-Key': this.ApiKey
                }
            })
            .then((response) => response.json())
            .then((responseJson) => {
                return responseJson;
            })
            .catch((error) => {
                console.error(error);
            });
    }

    async analyzeImageAsync(projectId) {
        const iteractions = await getIterationsAsync(projectId);

    }

    async detectImageFromFile(projectId, publishedName, file) {
        const url = this.BaseURL + 'prediction/' + projectId + '/detect/iterations/' + publishedName + '/image';

        return RNFetchBlob.fetch('POST', url, {
            'Content-Type': 'multipart/form-data',
            'Prediction-Key': this.ApiKey
        }, file)
        .then((response) => {
            return response.json();
        })
        .then((responseJson) => {
            return responseJson;
        })
        .catch((err) => {
            console.error(error);
        });
    }

    async detectImageFromCameraPicture(projectId, publishedName, picture) {
        const url = this.BaseURL + 'prediction/' + projectId + '/detect/iterations/' + publishedName + '/image';
        let file = RNFetchBlob.wrap(picture.uri.split('//')[1]);

        return RNFetchBlob.fetch('POST', url, {
            'Content-Type': 'multipart/form-data',
            'Prediction-Key': this.ApiKey
        }, file)
        .then((response) => {
            return response.json();
        })
        .then((responseJson) => {
            return responseJson;
        })
        .catch((err) => {
            console.error(error);
        });
    }

    async detectImageUrl(projectId, publishedName, imageUrl){
        // let data = new FormData();
        // data.append('', file, 'image.jpg');
        const url = this.BaseURL + 'prediction/' + projectId + '/detect/iterations/' + publishedName + '/url';
        return fetch(url, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Prediction-Key': this.ApiKey
            },
            body: JSON.stringify({url: imageUrl})
        })
        .then((response) => {
            return response.json();
        })
        .then((responseJson) => {
            return responseJson;
        })
        .catch((error) => {
            console.error(error);
        });
    }
}