import React from 'react'
import { View, Text, Image, Picker, FlatList, TouchableHighlight, Alert } from 'react-native';
import { NavigationScreenProp, NavigationState, NavigationParams } from 'react-navigation'
import ImagePicker, { ImagePickerOptions } from 'react-native-image-picker';
import Icon from 'react-native-vector-icons/FontAwesome'
import { SpecData, ImagePickerType } from '../../models';
import CustomSpecsDataLoader from '../../services/customSpecsDataLoader'
import { styles } from './InputScreen.style'
import CustomVisionService from '../../services/customVisionServiceHelper';
import RNFetchBlob from 'rn-fetch-blob';
Icon.loadFont()

interface InputScreenProps {
    navigation: NavigationScreenProp<NavigationState, NavigationParams>;
}

interface InputScreenState {
    specsData: Array<SpecData>;
    selectedSpecId: string;
    selectedSpec?: SpecData;
    suggestedImages: Array<any>;
}

export class InputScreen extends React.Component<InputScreenProps, InputScreenState> {
    static navigationOptions = ({ navigation } : { navigation : NavigationScreenProp<NavigationState,NavigationParams> }) => {
        const { params } = navigation.state;
        return { 
            title: 'Shelf Audit',
            headerRight: () => (
                <Icon.Button name="gear" size={25}
                    backgroundColor="transparent" 
                    underlayColor="transparent"
                    disabled={true}
                    color="gray"
                    onPress={() => { 
                        if (params?.openSettings) {
                            params.openSettings();
                        }
                    }} 
                />
            )
        }
    }

    private customSpecsDataLoader: CustomSpecsDataLoader;
    private imagePickerOptions: ImagePickerOptions;

    constructor(props: InputScreenProps) {
        super(props);

        this.customSpecsDataLoader = new CustomSpecsDataLoader();
        this.state = { 
            specsData: Array<SpecData>(),
            selectedSpecId: '',
            selectedSpec: undefined,
            suggestedImages: []
        };

        /*
         * Image Picker Options:
         * https://github.com/react-native-community/react-native-image-picker/blob/master/docs/Reference.md#options
         */ 
        this.imagePickerOptions = {
            title: 'Select a photo',
            storageOptions: {
                skipBackup: true,
                path: 'images',
            }
        };

        // event handlers
        this.handleImageClick = this.handleImageClick.bind(this);
    }

    async componentDidMount() {
        const { navigation } = this.props;
        navigation.setParams({ openSettings: this.openSettings });

        try {
            const data = this.customSpecsDataLoader.getLocalData();
            const items = data[0].SampleImages.map((v, i) => {
                return { id: i, src: v };
            });

            this.setState({
                selectedSpec: data[0],
                suggestedImages: items,
                specsData: data
            });

            // await this.customSpecsDataLoader.getData().then(data => {
            //     console.log('componentDidMount: get specs data: ', data);
            //     this.setState({specsData: data});
            // });
        } catch (e) {
            console.log(e);
            Alert.alert('Error getting specs');
        }
    }

    render() {
        const { navigate } = this.props.navigation;

        let mainView = 
            <View style={styles.container}>
                <View style={[styles.horizontalContainer, { marginTop: 20 }]}>
                    <Text style={styles.h3}>Shelf type</Text>
                    
                    <Picker 
                        style={styles.picker}
                        itemStyle={{ color: 'white' }}
                        selectedValue={this.state.selectedSpecId}
                        onValueChange={(itemValue, itemIndex) => {
                            this.setState({ selectedSpecId: itemValue });

                            this.state.specsData.forEach(obj => {
                                if (obj.Id === itemValue) {
                                    const items = obj.SampleImages.map((v: any, i: number) => {
                                        return { id: i, src: v };
                                    });
                                    this.setState({ 
                                        selectedSpec: obj,
                                        suggestedImages: items 
                                    });
                                }
                            });
                        }}
                    >
                        {this.state.specsData.map(spec => (
                            <Picker.Item key={spec.Id} label={`${spec.Name}`} value={spec.Id}/>
                        ))}
                    </Picker>
                </View>

                <View style={styles.line}/>

                <View style={{flex:1, paddingTop: 50 }}>
                    <View style={styles.centerContainer}>
                        <Text style={styles.h2}>Choose a shelf image to audit</Text>
                    </View>

                    <View style={{ flexDirection: 'row', alignItems: 'center', paddingLeft: 10, paddingRight: 10 }}>
                        <Text style={{ flex: 1, color: 'white', fontSize: 15 }}>Examples</Text>    
                        <Icon.Button name="image" size={30} style={{ alignContent: 'flex-end', justifyContent: 'flex-end' }}
                            backgroundColor="transparent" 
                            underlayColor="transparent"
                            color="white"
                            onPress={(e) => this.openImageGallery()}/>
                        <Icon.Button name="camera" size={30} style={{ alignContent: 'flex-end', justifyContent: 'flex-end' }}
                            backgroundColor="transparent" 
                            underlayColor="transparent"
                            color="white"
                            onPress={(e) => {
                                navigate('Camera', { specData: this.state.selectedSpec }); 
                                // navigate('Test');
                            }}/>
                    </View>
                    
                    <FlatList
                        data={this.state.suggestedImages}
                        renderItem={({ item }) => (
                            <View style={{ flex: 1, flexDirection: 'column', margin: 6 }}>
                                <TouchableHighlight onPress={(e) => { this.handleImageClick(item.src, ImagePickerType.FromWeb) }}>
                                    <Image style={styles.imageThumbnail} source={{ uri: item.src }} />
                                </TouchableHighlight>
                            </View>
                        )}
                        //Setting the number of column
                        numColumns={2}
                        keyExtractor={(item, index) => index.toString()}
                    />
                </View>
            </View>
    
        return mainView;
    }

    openImageGallery() {
        ImagePicker.launchImageLibrary(this.imagePickerOptions, async (response) => {
            if (response.didCancel) {
                console.log('User cancelled image picker');
            } else if (response.error) {
                console.log('ImagePicker Error: ', response.error);
            } else if (response.customButton) {
                console.log('User tapped custom button: ', response.customButton);
            } else {
                this.handleImageClick(response, ImagePickerType.FromImageGallery);
            }
        });
    }

    handleImageClick(image: any, imagePickerType: ImagePickerType) {
        const { navigate } = this.props.navigation;
        navigate('Review', {image: image, selectedSpec: this.state.selectedSpec, imagePickerType: imagePickerType});
    }

    openSettings() {
        Alert.alert('This is a settings!')
    }
}
