import React from 'react'
import { View, Text, Image, Picker, FlatList, TouchableHighlight, Alert } from 'react-native';
import { NavigationScreenProp, NavigationState, NavigationParams } from 'react-navigation'
import Icon from 'react-native-vector-icons/FontAwesome'
import { SpecData } from '../../models';
import CustomSpecsDataLoader from '../../services/customSpecsDataLoader'
import { styles } from './InputScreen.style'
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

    customSpecsDataLoader: CustomSpecsDataLoader;

    constructor(props: InputScreenProps) {
        super(props);

        this.customSpecsDataLoader = new CustomSpecsDataLoader();
        this.state = { 
            specsData: Array<SpecData>(),
            selectedSpecId: '',
            selectedSpec: undefined,
            suggestedImages: []
        };

        // event handlers
        this.handleImageClick = this.handleImageClick.bind(this);
    }

    async componentDidMount() {
        const { navigation } = this.props;
        navigation.setParams({ openSettings: this.openSettings });

        try {
            let data = this.customSpecsDataLoader.getLocalData();
            let items = data[0].SampleImages.map((v, i) => {
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
                                if (obj.Id == itemValue) {
                                    let items = obj.SampleImages.map((v: any, i: number) => {
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
                                <TouchableHighlight onPress={(e) => { this.handleImageClick(item) }}>
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

    handleImageClick(image: any) {
        const { navigate } = this.props.navigation;
        navigate('Review', {image: image, selectedSpec: this.state.selectedSpec});
    }

    openSettings() {
        Alert.alert('This is a settings!')
    }
}
