import React, { Component } from 'react';
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

export class InputScreen extends React.Component {
    constructor(props) {
        super(props);

        this.customSpecsDataLoader = new CustomSpecsDataLoader();
        this.state = { 
            specsData: [],
            selectedSpec: {},
            suggestedImages: []
        };

        // event handlers
        this.handleImageClick = this.handleImageClick.bind(this);
    }

    async componentDidMount() {
        try {
            let data = this.customSpecsDataLoader.getLocalData();
            this.setState({specsData: data});

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
                <View style={horizontalContainer}>
                    <Text style={h3}>Shelf type</Text>
                    
                    <Picker 
                        style={picker}
                        selectedValue={this.state.selectedSpec}
                        onValueChange={(itemValue, itemIndex) => {
                            let items = itemValue.SampleImages.map((v, i) => {
                                return { id: i, src: v };
                            });
                            this.setState({
                                selectedSpec: itemValue,
                                suggestedImages: items
                            });
                            console.log('Picker: onValueChange: ', items);
                        }}
                    >
                        {this.state.specsData.map(spec => (
                            <Picker.Item key={spec.Id} label={`${spec.Name}`} value={spec}/>
                        ))}
                    </Picker>
                </View>

                <View style={line}/>

                <View style={{flex:1, paddingTop: 20}}>
                    <View style={centerContainer}>
                        <Text style={h2}>Choose a shelf image to audit</Text>
                    </View>

                    <View style={{ flexDirection: 'row', alignItems: 'center', paddingLeft: 10, paddingRight: 10 }}>
                        <Text style={{ flex: 1, color: 'white', fontSize: 15 }}>Examples</Text>    
                        <Icon.Button name="camera" size={30} style={{ alignContent: 'flex-end', justifyContent: 'flex-end' }}
                            backgroundColor="transparent" 
                            underlayColor="transparent"
                            color="white"
                            onPress={(e) => { navigate('Camera', { specData: this.state.selectedSpec }); }}/>
                    </View>
                    
                    <FlatList
                        data={this.state.suggestedImages}
                        renderItem={({ item }) => (
                            <View style={{ flex: 1, flexDirection: 'column', margin: 4 }}>
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
