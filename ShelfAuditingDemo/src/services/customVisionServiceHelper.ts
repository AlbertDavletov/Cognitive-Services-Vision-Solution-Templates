import RNFetchBlob from 'rn-fetch-blob';

export default class CustomVisionService {
    private readonly BaseURL = '<ENPOINT>';
    private readonly ApiKey = '<KEY>';

    async getTagsAsync(projectId: string) {
        const url = this.BaseURL + 'training/projects/' + projectId + '/tags';
        return fetch(url,
            {
                method: 'GET',
                headers: {
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

    async getIterationsAsync(projectId: string) {
        const url = this.BaseURL + 'training/projects/' + projectId + '/iterations';
        return fetch(url,
            {
                method: 'GET',
                headers: {
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

    async detectImageFromFile(projectId: string, publishedName: string, file: File) {
        const url = this.BaseURL + 'prediction/' + projectId + '/detect/iterations/' + publishedName + '/image';

        return RNFetchBlob.fetch('POST', url, {
            'Content-Type': 'multipart/form-data',
            'Prediction-Key': this.ApiKey
        }, [ file ])
        .then((response) => {
            return response.json();
        })
        .then((responseJson) => {
            return responseJson;
        })
        .catch((err) => {
            console.error(err);
        });
    }

    async detectImageFromCameraPicture(projectId: string, publishedName: string, picture: string) {
        const url = this.BaseURL + 'prediction/' + projectId + '/detect/iterations/' + publishedName + '/image';
        let file = RNFetchBlob.wrap(picture.split('//')[1]);

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
            console.error(err);
        });
    }

    async detectImageUrl(projectId: string, publishedName: string, imageUrl: string) {
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
