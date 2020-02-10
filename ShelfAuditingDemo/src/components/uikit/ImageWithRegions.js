import React, { Component } from 'react';
import { View, Image, ImageBackground, TouchableOpacity, Animated, StyleSheet } from 'react-native';
import ReactNativeZoomableView from '@dudigital/react-native-zoomable-view/src/ReactNativeZoomableView';
import { RegionState } from '../../models';
import { ObjectRegion } from './';

class ImageWithRegions extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            imageDimWidth: undefined,
            imageDimHeight: undefined,
            predictions: [],
            selectedRegions: {}
        };

        // event handlers
        this.handleImageLayout = this.handleImageLayout.bind(this);
    }

    componentDidMount() {
        const { imageSource, regions } = this.props;
        Image.getSize(imageSource, (width, height) => {
            this.setState({
                imageDimWidth: width,
                imageDimHeight: height
            });
        });

        if (regions) {
            this.setState({ 
                regions: regions
            });
        }
    }

    render() {
        const { imageContainer, canvasContainer, h1, image } = this.styles;
        const { imageSource } = this.props;

        return (
            <View style={imageContainer}>

                <ReactNativeZoomableView
                    maxZoom={2.5}
                    minZoom={1}
                    zoomStep={0.5}
                    initialZoom={1}
                    bindToBorders={true}
                >
                    <ImageBackground style={image}
                        onLayout={(event) => this.handleImageLayout(event)}
                        resizeMode={'contain'}
                        source={{uri: imageSource}}>

                        <View style={[canvasContainer, { width: this.state.imageWidth, height: this.state.imageHeight}]}>
                            {this.getImageWithRegionsComponent()}
                        </View>

                    </ImageBackground>
                </ReactNativeZoomableView>
            </View>
        );
    }

    getImageWithRegionsComponent() {
        let component; 
        let enable = this.state.regions && this.state.imageWidth && this.state.imageHeight;

        if (enable) {

            component = this.state.regions.map((obj, ind) => {
                const model = obj.model;
                const imageWidth = this.state.imageWidth;
                const imageHeight = this.state.imageHeight;

                let l = model.boundingBox.left * imageWidth;
                let t = model.boundingBox.top * imageHeight;
                let w = model.boundingBox.width * imageWidth;
                let h = model.boundingBox.height * imageHeight;

                if (!isNaN(l) && !isNaN(t) && !isNaN(w) && !isNaN(h)) {

                    return <TouchableOpacity key={obj.id} onPress={() => this.onRegionSelected(obj)} activeOpacity={0.6}
                                style={[ this.styles.touchableContainer, 
                                    { left: l, top: t, width: w, height: h, 
                                    zIndex: this.state.selectedRegions[obj.id] == RegionState.Selected ? 10 : 1
                                }]}>
                                    
                                <ObjectRegion 
                                    position={{ width: w, height: h }} 
                                    data={{ 
                                        state: this.state.selectedRegions[obj.id], 
                                        title: obj.displayName,
                                        product: obj,
                                    }}
                                />
                            </TouchableOpacity>;
                }                          
            });
        }

        return component;
    }

    // event handlers
    handleImageLayout(event) {
        console.log('handleImageLayout: ', event.nativeEvent.layout);

        const containerHeight = event.nativeEvent.layout.height;
        const containerWidth = event.nativeEvent.layout.width;
        const dimWidth = this.state.imageDimWidth;
        const dimHeight = this.state.imageDimHeight;

        const newImageWidth = containerHeight * dimWidth / dimHeight <= containerWidth ? containerHeight * dimWidth / dimHeight : containerWidth;
        const newImageHeight = containerWidth * dimHeight / dimWidth <= containerHeight ? containerWidth * dimHeight / dimWidth : containerHeight;

        this.setState({
            imageWidth: newImageWidth,
            imageHeight: newImageHeight
        });
        this.forceUpdate();
    }

    onRegionSelected(region) {
        let state = this.state.selectedRegions[region.id];
        this.setState(prevState => ({
            selectedRegions : {
                ...prevState.selectedRegions,
                [region.id]: state == RegionState.Selected ? RegionState.Active : RegionState.Selected
            }
        }));
    }

    clearSelection() {
        const regions = this.state.selectedRegions;
        for (let prop in regions) {
            regions[prop] = RegionState.Active;
        }
        this.setState({selectedRegions : regions});
    }

    styles = StyleSheet.create({
        imageContainer: {
            flex: 1
        },
        image: {
            flex: 1,
            justifyContent: 'center',
            transform: [{ scale: 1 }]
        },
        touchableContainer: {
            position: 'absolute',
            justifyContent: 'center'
        },
        canvasContainer: {
            alignSelf: 'center',
            backgroundColor: 'transparent',
        },
        h1: {
            color: 'white',
            fontSize: 15,
        },
    })
}

export { ImageWithRegions };