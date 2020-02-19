import React, { Component } from 'react';
import { View, Image, Text, TouchableOpacity, StyleSheet } from 'react-native';
import { ImageWithRegions } from '../components/uikit';

class AddEditScreen extends React.Component {

    constructor(props) {
        super(props);
    }

    render() {
        const { navigation } = this.props;
        const { mainContainer, labelContainer, imageThumbnail, button, buttonLabel,
            tagContainer, tagLabel, line } = this.styles;
        let data = navigation.getParam('data', []);
        let selectedRegions = navigation.getParam('selectedRegions', []);
        let imageSource = navigation.getParam('imageSource', {});

        let selectedTag;
        if (selectedRegions.length > 0) {
            let model = selectedRegions[0].model;
            selectedTag = {
                id: model.tagId,
                name: model.tagName,
                imageUrl: 'https://intelligentkioskstore.blob.core.windows.net/shelf-auditing/Mars/Products/' + model.tagName.toLocaleLowerCase() + '.jpg'
            };
        }
        console.log('selectedTag: ', selectedTag);

        let imageWithRegionsComponent;
        if (data) {
            imageWithRegionsComponent = (
                <ImageWithRegions ref='imageWithRegions'
                    mode='edit'
                    imageSource={imageSource}
                    regions={data}
                    editableRegions={selectedRegions}
                />
            );
        }

        return (
            <View style={mainContainer}>

                { imageWithRegionsComponent }
                
                <View style={labelContainer}>
                    <TouchableOpacity activeOpacity={0.6} onPress={() => this.onChooseLabel()} style={button}>
                        <Text style={buttonLabel}>Choose new label</Text>
                    </TouchableOpacity>

                    <View style={{ flexDirection: 'row'}}>
                        <View style={{ padding: 10 }}>
                            { selectedTag.name.toLocaleLowerCase() != 'product' && selectedTag.name.toLocaleLowerCase() != 'gap' &&
                                <Image style={imageThumbnail} source={{ uri: selectedTag.imageUrl }} />
                            }
                            { selectedTag.name.toLocaleLowerCase() == 'product' &&
                                <Image style={imageThumbnail} source={require('../assets/product.jpg')} />
                            }
                            { selectedTag.name.toLocaleLowerCase() == 'gap' &&
                                <Image style={imageThumbnail} source={require('../assets/gap.jpg')} />
                            }
                        </View>
                        
                        <View style={{ flex: 1 }}>
                            <View style={tagContainer}>
                                <Text numberOfLines={1} style={tagLabel}>{selectedTag.name}</Text>
                            </View>
                            
                            <View style={line}/>
                        </View>
                        
                    </View>
                </View>
            </View>
        );
    }

    onChooseLabel() {
        const { navigation } = this.props;
        console.log('Choose new label');
    }

    styles = StyleSheet.create({
        mainContainer: {
            flex: 1,
            backgroundColor: 'black',
            padding: 0
        },
        labelContainer: {
            height: '25%',
            padding: 16
        },
        imageThumbnail: {
            justifyContent: 'center',
            alignItems: 'center',
            height: 40,
            width: 40
        },
        button: {
            padding: 6, 
            marginBottom: 10
        },
        buttonLabel: {
            color: '#0078D4', 
            fontSize: 16, 
            fontWeight: 'bold', 
            textAlign: 'center' 
        },
        tagContainer: {
            flex: 1, 
            justifyContent: 'center'
        },
        tagLabel: {
            color: 'white', 
            textAlign: 'left', 
            padding: 5
        },
        line: {
            height: 1, 
            backgroundColor: 'white', 
            opacity: 0.2
        }
    })
}

export { AddEditScreen };