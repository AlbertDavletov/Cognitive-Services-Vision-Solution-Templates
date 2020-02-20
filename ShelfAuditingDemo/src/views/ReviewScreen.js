import React, { Component } from 'react';
import { View, ActivityIndicator, StyleSheet, Alert } from 'react-native';
import { ProductItem } from '../models';
import { ImageWithRegions } from '../components/uikit';
import Icon from 'react-native-vector-icons/FontAwesome';
import CustomVisionService from '../services/customVisionServiceHelper';
Icon.loadFont();

class ReviewScreen extends React.Component {
    constructor(props) {
        super(props);
        this.customVisionService = new CustomVisionService();

        this.state = {
            loading: false,
            detectedObjects: null,
            imageSource: null,

            editRegionButtonStyle: {
                disabled: true,
                color: 'gray'
            },
            removeRegionButtonStyle: {
                disabled: true,
                color: 'gray'
            },
            clearRegionsButtonStyle: {
                disabled: true,
                color: 'gray'
            }
        };

        // event handlers
        this.onRegionSelectionChanged = this.onRegionSelectionChanged.bind(this);
    }

    async componentDidMount() {
        const { navigation } = this.props;
        let imageProps = navigation.getParam('image', 'unknown');
        let specData = navigation.getParam('selectedSpec', 'unknown');
        let fromCamera = navigation.getParam('fromCamera', false);

        let imageSrc = imageProps.src ? imageProps.src : imageProps;
        this.setState({ imageSource: imageProps.src ? imageProps.src : imageProps.uri });
        const data = await this.analyzeImage(specData.ModelId, imageSrc, fromCamera);
        const tags = await this.getTags(specData.ModelId);
        // const data = this.getTestDataFromJson();

        let detectedObjects = [];
        if (data.predictions) {
            data.predictions.map((val, ind) => {
                let productItem = new ProductItem(val);
                detectedObjects.push(productItem);
            });    
        }
        
        this.setState({ 
            detectedObjects: detectedObjects,
            specData: specData,
            tags: tags
        });
    }

    render() {
        const { mainContainer, h1, loading } = this.styles;

        let imageWithRegionsComponent;
        if (this.state.detectedObjects) {
            imageWithRegionsComponent = (
                <ImageWithRegions ref='imageWithRegions'
                    selectionChanged={(selectedCount) => this.onRegionSelectionChanged(selectedCount)}
                    imageSource={this.state.imageSource}
                    regions={this.state.detectedObjects}
                />
            );
        }

        return (
            <View style={mainContainer}>

                { imageWithRegionsComponent }

                { !this.state.loading && 
                    <View style={{
                            backgroundColor: '#1F1F1F',
                            height: 49, 
                            justifyContent: 'space-around',
                            flexDirection: 'row',            
                        }}>
                        <Icon.Button name="bar-chart" size={20} style={{flex: 1}}
                            backgroundColor="transparent"
                            underlayColor="transparent"
                            onPress={() => {
                                const { navigate } = this.props.navigation;
                                navigate('Result', { data: this.state.detectedObjects, specData: this.state.specData });
                            }} 
                        />

                        <Icon.Button name="plus" size={20} style={{flex: 1}}
                            backgroundColor="transparent" 
                            underlayColor="transparent"
                            disabled={true}
                            color="gray"
                            onPress={() => alert('Add product!')} />

                        <Icon.Button name="pencil" size={20} style={{flex: 1}}
                            ref='editRegionButton'
                            backgroundColor="transparent" 
                            underlayColor="transparent"
                            disabled={this.state.editRegionButtonStyle.disabled}
                            color={this.state.editRegionButtonStyle.color}
                            onPress={() => {
                                const { navigate } = this.props.navigation;
                                let selectedRegions = this.refs.imageWithRegions.getSelectedRegions();
                                let selectedIds = selectedRegions.map(item => item.id);
                                let data = this.state.detectedObjects.filter((obj) => {
                                    return selectedIds.indexOf(obj.id) < 0;
                                });
                                navigate('AddEdit', { 
                                    data: data, 
                                    selectedRegions: selectedRegions, 
                                    tags: this.state.tags,
                                    imageSource: this.state.imageSource 
                                });
                            }} />

                        <Icon.Button name="trash-o" size={20} style={{ flex: 1 }}
                            ref='removeRegionButton'
                            backgroundColor="transparent" 
                            underlayColor="transparent"
                            disabled={this.state.removeRegionButtonStyle.disabled}
                            color={this.state.removeRegionButtonStyle.color}
                            onPress={() => alert('Remove product!')} />

                        <Icon.Button name="refresh" size={20} style={{ flex: 1 }}
                            ref='clearRegionsButton'
                            backgroundColor="transparent" 
                            underlayColor="transparent"
                            disabled={this.state.clearRegionsButtonStyle.disabled}
                            color={this.state.clearRegionsButtonStyle.color}
                            onPress={() => {
                                this.refs.imageWithRegions.clearSelection();
                            }} />
                    </View>
                }

                {this.state.loading &&
                    <View style={loading}>
                        <ActivityIndicator size='large' color='blue' />
                    </View>
                }
            </View>
        );
    }

    getTestDataFromJson() {
        return JSON.parse('{"id":"c851e44a-70df-4f38-aa17-dd47b92768e0","project":"af826f5b-97c1-40a0-b8bb-bf44e08cec2b","iteration":"0e3a55b8-6da5-44e6-b392-0d676b9b0fd4","created":"2020-01-29T22:13:57.384Z","predictions":[{"probability":0.9937126,"tagId":"2d181dc2-e460-4424-aeeb-5a09e1345019","tagName":"Pedigree-DENTASTIX-R-PEQUENAS","boundingBox":{"left":0.316826,"top":0.280495048,"width":0.04651743,"height":0.08621499}},{"probability":0.995830238,"tagId":"2d181dc2-e460-4424-aeeb-5a09e1345019","tagName":"Pedigree-DENTASTIX-R-PEQUENAS","boundingBox":{"left":0.3709323,"top":0.28188628,"width":0.0459396839,"height":0.08474082}},{"probability":0.996776044,"tagId":"2d181dc2-e460-4424-aeeb-5a09e1345019","tagName":"Pedigree-DENTASTIX-R-PEQUENAS","boundingBox":{"left":0.4276399,"top":0.282798171,"width":0.0533616245,"height":0.0832499862}},{"probability":0.9986719,"tagId":"2d181dc2-e460-4424-aeeb-5a09e1345019","tagName":"Pedigree-DENTASTIX-R-PEQUENAS","boundingBox":{"left":0.489086032,"top":0.2825645,"width":0.0539793968,"height":0.0822044}},{"probability":0.9997098,"tagId":"75d492e8-1e64-4b9b-95fa-d72ac7102e2f","tagName":"Pedigree-DOG-SCH-AD-RM-CAR-MOL","boundingBox":{"left":0.6647398,"top":0.4203011,"width":0.0841907859,"height":0.0584433675}},{"probability":0.993088663,"tagId":"75d492e8-1e64-4b9b-95fa-d72ac7102e2f","tagName":"Pedigree-DOG-SCH-AD-RM-CAR-MOL","boundingBox":{"left":0.7429953,"top":0.42478767,"width":0.0941785,"height":0.06053102}},{"probability":0.967658341,"tagId":"81ecb2cb-8e1a-495a-988e-bbb8f2180cbc","tagName":"product","boundingBox":{"left":0.958388865,"top":0.4424228,"width":0.041611135,"height":0.0567141771}},{"probability":0.9513777,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.495861262,"top":0.0126488879,"width":0.128105611,"height":0.164716989}},{"probability":0.9544068,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.632092834,"top":0.00479023159,"width":0.139049411,"height":0.175763667}},{"probability":0.974745154,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.785126269,"top":0.0003399551,"width":0.159193456,"height":0.18067877}},{"probability":0.931115448,"tagId":"bd4e09fd-6b94-43ed-b6bd-d3dee2ff91fe","tagName":"Champ-FILHOTES","boundingBox":{"left":0.0530773029,"top":0.0512277558,"width":0.104126073,"height":0.135507941}},{"probability":0.9864905,"tagId":"bd4e09fd-6b94-43ed-b6bd-d3dee2ff91fe","tagName":"Champ-FILHOTES","boundingBox":{"left":0.15929243,"top":0.0445011258,"width":0.107162148,"height":0.137035534}},{"probability":0.9827475,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.2770788,"top":0.0408046767,"width":0.103809059,"height":0.141547412}},{"probability":0.9681962,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.38888222,"top":0.0310575515,"width":0.107008338,"height":0.148627341}},{"probability":0.6219323,"tagId":"cf0a7df3-ceaa-4eed-8350-a056160f4a1c","tagName":"Pedigree-DRY-AD-CARNE&VEGETAIS","boundingBox":{"left":0.00460821,"top":0.211000144,"width":0.09814942,"height":0.168409646}},{"probability":0.597254336,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.113837577,"top":0.253066361,"width":0.08329936,"height":0.1323682}},{"probability":0.9743201,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.21157831,"top":0.256551772,"width":0.07710391,"height":0.135931015}},{"probability":0.456437051,"tagId":"4f1a4791-4552-4a7c-a47c-6f62d1add1c7","tagName":"PEDIGREE-SCH-MPK-AD-RP-CARNE-MO","boundingBox":{"left":0.5601845,"top":0.261541,"width":0.06021464,"height":0.15899241}},{"probability":0.9640249,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.6167337,"top":0.258145571,"width":0.125221968,"height":0.157861143}},{"probability":0.9917163,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.7582977,"top":0.26638636,"width":0.149484813,"height":0.156571865}},{"probability":0.990387142,"tagId":"cf0a7df3-ceaa-4eed-8350-a056160f4a1c","tagName":"Pedigree-DRY-AD-CARNE&VEGETAIS","boundingBox":{"left":0.005964294,"top":0.402701676,"width":0.09202298,"height":0.171302736}},{"probability":0.9998698,"tagId":"75d492e8-1e64-4b9b-95fa-d72ac7102e2f","tagName":"Pedigree-DOG-SCH-AD-RM-CAR-MOL","boundingBox":{"left":0.8427975,"top":0.435560524,"width":0.0975968838,"height":0.07301271}},{"probability":0.538856566,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.103152484,"top":0.446311474,"width":0.08721048,"height":0.140190482}},{"probability":0.7764122,"tagId":"bd4e09fd-6b94-43ed-b6bd-d3dee2ff91fe","tagName":"Champ-FILHOTES","boundingBox":{"left":0.20072946,"top":0.455625176,"width":0.09575263,"height":0.139037013}},{"probability":0.9935655,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.305360526,"top":0.4715278,"width":0.101005942,"height":0.136644721}},{"probability":0.989527464,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.410181135,"top":0.4759163,"width":0.104801983,"height":0.143414825}},{"probability":0.9827697,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.522232056,"top":0.499151826,"width":0.112317741,"height":0.142846227}},{"probability":0.96819824,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.6343259,"top":0.5100056,"width":0.131547213,"height":0.151346326}},{"probability":0.980216563,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.772573769,"top":0.52739495,"width":0.136932909,"height":0.151243448}},{"probability":0.9128154,"tagId":"cf0a7df3-ceaa-4eed-8350-a056160f4a1c","tagName":"Pedigree-DRY-AD-CARNE&VEGETAIS","boundingBox":{"left":0,"top":0.6065591,"width":0.0852176845,"height":0.202873945}},{"probability":0.975183,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.102261983,"top":0.6040927,"width":0.1947161,"height":0.276206136}},{"probability":0.9158497,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.321403325,"top":0.652238,"width":0.272292316,"height":0.272640049}},{"probability":0.989137053,"tagId":"9679143f-5aed-4060-9b4f-3ef6500379a8","tagName":"Champ-ADULTO-CARNE-&-CEREAL","boundingBox":{"left":0.6047908,"top":0.697729349,"width":0.291805267,"height":0.297402859}}]}');
    }

    async analyzeImage(projectId, imageSrc, fromCamera) {
        let result = [];
        try {
            this.setState({ loading: true });
            
            const iterations = await this.customVisionService.getIterationsAsync(projectId);
            result = await this.analyzeImageByIteration(iterations, imageSrc, fromCamera);
        } catch (error) {
            console.error('CustomVisionService - analyzeImage()', error);
        } finally {
            this.setState({ loading: false });
        }
        return result;
    }

    async analyzeImageByIteration(data, imageSrc, fromCamera) {
        let result = [];
        
        if (data && data.length) {
            const filteredData = data.filter(function(item) {
                return item.status == 'Completed';
              });
          
              // sort by descending
              filteredData.sort(function(a,b){
                return new Date(b.trainedAt) - new Date(a.trainedAt);
              })
          
              const latestTrainedIteraction = filteredData.length > 0 ? filteredData[0] : null;
              if (latestTrainedIteraction == null || !latestTrainedIteraction.publishName) {
                console.log("This project doesn't have any trained models or published iteration yet. Please train and publish it, or wait until training completes if one is in progress.");
      
              } else {
      
                  console.log("latestTrainedIteraction: ", latestTrainedIteraction);
          
                  if (fromCamera) {
                      await this.customVisionService.detectImageFromCameraPicture(latestTrainedIteraction.projectId, latestTrainedIteraction.publishName, imageSrc)
                      .then((responseJson) => {
                          result = responseJson;
                      })
                      .catch((error) => {
                          console.error('detectImage', error);
                      });
                  } else {
                      await this.customVisionService.detectImageUrl(latestTrainedIteraction.projectId, latestTrainedIteraction.publishName, imageSrc)
                      .then((responseJson) => {
                          result = responseJson;
                      })
                      .catch((error) => {
                          console.error('detectImage', error);
                      });
                  }
              }
        }

        return result;
    }

    async getTags(projectId) {
        let tags = [];
        try {
            tags = await this.customVisionService.getTagsAsync(projectId);
        } catch (error) {
            console.error('CustomVisionService - analyzeImage()', error);
        }
        return tags;
    }

    onRegionSelectionChanged(selectedCount) {
        this.setState({
            editRegionButtonStyle: {
                disabled: selectedCount <= 0,
                color: selectedCount > 0 ? 'white' : 'gray'
            },
            removeRegionButtonStyle: {
                disabled: selectedCount <= 0,
                color: selectedCount > 0 ? 'white' : 'gray'
            },
            clearRegionsButtonStyle: {
                disabled: selectedCount <= 0,
                color: selectedCount > 0 ? 'white' : 'gray'
            }
        });
    }

    styles = StyleSheet.create({
        mainContainer: {
            flex: 1, 
            // alignItems: 'stretch',
            backgroundColor: 'black'
        },
        h1: {
            color: 'white',
            fontSize: 17,
            padding: 20
        },
        loading: {
            position: 'absolute',
            left: 0,
            right: 0,
            top: 0,
            bottom: 0,
            alignItems: 'center',
            justifyContent: 'center',
            backgroundColor: 'black',
            opacity: 0.7
        },
    })
}

export { ReviewScreen };