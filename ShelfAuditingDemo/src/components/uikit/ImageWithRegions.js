import React, { Component } from 'react';
import { View, Text, Image, ImageBackground, TouchableOpacity, Animated, StyleSheet, Alert } from 'react-native';
import ReactNativeZoomableView from '@dudigital/react-native-zoomable-view/src/ReactNativeZoomableView';
import { Util } from '../../../Util';
import { RegionState } from '../../models';
import { ObjectRegion, EditableRegion } from './';

class ImageWithRegions extends React.Component {
    constructor(props) {
        super(props);
        this.state = {
            imageDimWidth: undefined,
            imageDimHeight: undefined,
            predictions: [],
            editableRegions: [],
            selectedRegions: {},
            zoomLevel: 1,
            zoomOffsetX: 0,
            zoomOffsetY: 0,
            distanceLeft: 0,
            distanceTop: 0,
            enableZoom: true,
        };

        // event handlers
        this.handleImageLayout = this.handleImageLayout.bind(this);
    }

    componentDidMount() {
        const { imageSource, regions, editableRegions } = this.props;
        Image.getSize(imageSource, (width, height) => {
            this.setState({
                imageDimWidth: width,
                imageDimHeight: height
            });
        });

        if (regions) {
            this.setState({ 
                regions: regions,
                editableRegions: editableRegions
            });
        }
    }

    render() {
        const { imageContainer, canvasContainer, h1, image } = this.styles;
        const { imageSource, mode } = this.props;

        return (
            <View style={imageContainer}>

                <ReactNativeZoomableView
                    maxZoom={2.5}
                    minZoom={1}
                    zoomStep={0.5}
                    initialZoom={this.state.zoomLevel}
                    bindToBorders={true}
                    zoomEnabled={this.state.enableZoom}
                >                  
                    <ImageBackground
                        style={[image, { 
                            transform: [ 
                                { translateX: 0 }, 
                                { translateY: 0 }
                            ] 
                        }]}
                        onLayout={(event) => this.handleImageLayout(event)}
                        resizeMode={'contain'}
                        source={{uri: imageSource}}>

                        <View style={[canvasContainer, { width: this.state.imageWidth, height: this.state.imageHeight}]}>
                            {this.getImageWithRegionsComponent(mode)}

                            {mode == 'edit' && 
                                <View style={{ width: '100%', height: '100%', backgroundColor: 'rgba(0, 0, 0, 0.7)', overflow: 'hidden', zIndex: 20 }}>                                     
                                    {this.getImageWithEditableRegionsComponent()}
                                </View>
                            }
                            
                        </View>

                    </ImageBackground>
                </ReactNativeZoomableView>
            </View>
        );
    }

    getImageWithRegionsComponent(mode) {
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
                    let state = mode == 'edit' ? RegionState.Disabled : this.state.selectedRegions[obj.id];

                    return <TouchableOpacity key={obj.id} onPress={() => this.onRegionSelected(obj)} activeOpacity={0.6}
                                style={[ this.styles.touchableContainer, 
                                    { left: l, top: t, width: w, height: h, 
                                    zIndex: state == RegionState.Selected ? 10 : 5
                                }]}>
                                    
                                <ObjectRegion 
                                    position={{ width: w, height: h }} 
                                    data={{
                                        color: Util.GetObjectRegionColor(model),
                                        state: state, 
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

    getImageWithEditableRegionsComponent() {
        let component; 
        let enable = this.state.regions && this.state.imageWidth && this.state.imageHeight;

        if (enable) {
            component = this.state.editableRegions.map((obj, ind) => {
                const model = obj.model;
                const imageWidth = this.state.imageWidth;
                const imageHeight = this.state.imageHeight;

                let l = model.boundingBox.left * imageWidth;
                let t = model.boundingBox.top * imageHeight;
                let w = model.boundingBox.width * imageWidth;
                let h = model.boundingBox.height * imageHeight;

                if (!isNaN(l) && !isNaN(t) && !isNaN(w) && !isNaN(h)) {
                    return <EditableRegion key={obj.id}
                                position={{ left: l, top: t, width: w, height: h }} 
                                data={{
                                    title: obj.displayName,
                                    product: obj,
                                }}
                                positionChange={(change, position) => {
                                    obj.model.boundingBox = {
                                        left: position.left / imageWidth,
                                        top: position.top / imageHeight,
                                        width: position.width / imageWidth,
                                        height: position.height / imageHeight
                                    }
                                    this.setState({ enableZoom: !change });
                                }}
                            />;
                }                          
            });
        }

        return component;
    }

    // event handlers
    handleImageLayout(event) {
        const containerHeight = event.nativeEvent.layout.height;
        const containerWidth = event.nativeEvent.layout.width;
        this.setState({
            imageContainerWidth: containerWidth,
            imageContainerHeight: containerHeight
        });

        Image.getSize(this.props.imageSource, (width, height) => {

            const dimWidth = width;
            const dimHeight = height;
    
            const newImageWidth = containerHeight * dimWidth / dimHeight <= containerWidth ? containerHeight * dimWidth / dimHeight : containerWidth;
            const newImageHeight = containerWidth * dimHeight / dimWidth <= containerHeight ? containerWidth * dimHeight / dimWidth : containerHeight;
    
            this.setState({
                imageWidth: newImageWidth,
                imageHeight: newImageHeight
            });
            this.forceUpdate();
        });
    }

    onRegionSelected(region) {
        let state = this.state.selectedRegions[region.id];

        if (state != RegionState.Disabled) {
            this.setState(prevState => ({
                selectedRegions : {
                    ...prevState.selectedRegions,
                    [region.id]: state == RegionState.Selected ? RegionState.Active : RegionState.Selected
                }
            }));

            let selectedCount = state == RegionState.Selected ? 0 : 1;
            for (const [key, value] of Object.entries(this.state.selectedRegions)) {
                if (region.id != key && value == RegionState.Selected) {
                    selectedCount += 1;
                }
            }

            this.onSelectionChanged(selectedCount);
        }
    }

    clearSelection() {
        const regions = this.state.selectedRegions;
        for (let prop in regions) {
            regions[prop] = RegionState.Active;
        }
        this.setState({selectedRegions : regions});

        this.onSelectionChanged(0);
    }

    onSelectionChanged(selectedCount) {
        if (this.props.selectionChanged) {
            this.props.selectionChanged(selectedCount);
        }
    }

    getSelectedRegions() {
        let selectedRegions = this.state.selectedRegions;
        let selected = [];
        this.state.regions.forEach((region) => {
            if (selectedRegions[region.id] == RegionState.Selected) {
                let copy = JSON.parse(JSON.stringify(region));
                selected.push(copy);
            }
        });
        return selected;
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
            alignSelf: 'center'
        },
        h1: {
            color: 'white',
            fontSize: 15,
        },
    })
}

export { ImageWithRegions };