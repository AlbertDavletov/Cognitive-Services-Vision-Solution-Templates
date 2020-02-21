import React from 'react'
import { Component } from 'react'
import { 
    View, 
    Text, 
    Image,
    Picker, 
    FlatList,
    TouchableHighlight,
    StyleSheet, 
    Alert 
} from 'react-native';
import Icon from 'react-native-vector-icons/FontAwesome';
import CustomSpecsDataLoader from '../services/customSpecsDataLoader';
Icon.loadFont();

export class InputScreen extends React.Component {
    static navigationOptions = ({ navigation }) => {
        const { params } = navigation.state;
        return { 
            title: 'Shelf Audit',
            headerRight: () => (
                <Icon.Button name="gear" size={25}
                    backgroundColor="transparent" 
                    underlayColor="transparent"
                    disabled={true}
                    color="gray"
                    onPress={() => params.openSettings()} />
            )
        }
    }

    constructor(props) {
        super(props);

        this.customSpecsDataLoader = new CustomSpecsDataLoader();
        this.state = { 
            specsData: [],
            selectedSpecId: '',
            selectedSpec: {},
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
        const { container, h1, h2, h3, 
                centerContainer, horizontalContainer,
                picker, line, imageThumbnail } = this.styles;

        let mainView = 
            <View style={container}>
                <View style={[horizontalContainer, { marginTop: 20 }]}>
                    <Text style={h3}>Shelf type</Text>
                    
                    <Picker 
                        style={picker}
                        itemStyle={{ color: 'white' }}
                        selectedValue={this.state.selectedSpecId}
                        onValueChange={(itemValue, itemIndex) => {
                            this.setState({ selectedSpecId: itemValue });

                            this.state.specsData.forEach(obj => {
                                if (obj.Id == itemValue) {
                                    let items = obj.SampleImages.map((v, i) => {
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

                <View style={line}/>

                <View style={{flex:1, paddingTop: 50 }}>
                    <View style={centerContainer}>
                        <Text style={h2}>Choose a shelf image to audit</Text>
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
                                    <Image style={imageThumbnail} source={{ uri: item.src }} />
                                </TouchableHighlight>
                            </View>
                        )}
                        //Setting the number of column
                        numColumns={2}
                        keyExtractor={(item, index) => index}
                    />
                </View>
            </View>
    
        return mainView;
    }

    handleImageClick(image) {
        const { navigate } = this.props.navigation;
        navigate('Review', {image: image, selectedSpec: this.state.selectedSpec});
    }

    openSettings() {
        Alert.alert('This is a settings!')
    }

    styles = StyleSheet.create({
        container: {
            flex: 1, 
            alignItems: 'stretch',
            backgroundColor: 'black'
        },
        centerContainer: {
            alignItems: 'center'           
        },
        horizontalContainer: {
            flexDirection: 'row',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: 14,
            height: 54
        },
        h1: {
            color: 'white',
            fontSize: 17,
            padding: 20
        },
        h2: {
            color: 'white',
            fontSize: 15,
            padding: 10
        },
        h3: {
            color: '#0A84FF',
            fontSize: 13,
            fontWeight: 'bold'
        },
        picker: {
            flex: 1,
            alignItems: 'stretch',
            color: 'white',
            marginLeft: 10, 
            marginRight: 10
        },
        line: {
            borderBottomColor: 'white',
            opacity: 0.2,
            borderBottomWidth: 1,
        },
        imageThumbnail: {
            justifyContent: 'center',
            alignItems: 'center',
            height: 200
        }
    })
}
