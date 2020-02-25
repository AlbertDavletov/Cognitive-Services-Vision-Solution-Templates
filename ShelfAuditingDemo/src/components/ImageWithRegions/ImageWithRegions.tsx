import React from 'react'
import { View, Image, ImageBackground, TouchableOpacity, LayoutChangeEvent } from 'react-native'
import ReactNativeZoomableView from '@dudigital/react-native-zoomable-view/src/ReactNativeZoomableView'
import { ObjectRegion, EditableRegion } from '..'
import { RegionState } from '../../models'
import { Util } from '../../../Util'
import { styles } from './ImageWithRegions.style'

interface ImageProps {
    imageSource: any;
    mode: string;
    regions: Array<any>;
    editableRegions?: Array<any>;
    selectionChanged?: Function;
}
  
interface ImageState {
    imageDimWidth: number,
    imageDimHeight: number,
    imageWidth: number,
    imageHeight: number,
    predictions: [],
    editableRegions: [],
    selectedRegions: Array<any>,
    zoomLevel: number,
    enableZoom: boolean,
}

export class ImageWithRegions extends React.Component<ImageProps, ImageState>  {
    constructor(props: ImageProps) {
        super(props);
        this.state = {
            imageDimWidth: 0,
            imageDimHeight: 0,
            imageWidth: 0,
            imageHeight: 0,
            predictions: [],
            editableRegions: [],
            selectedRegions: Array<any>(),
            zoomLevel: 1,
            enableZoom: true,
        };

        // event handlers
        this.handleImageLayout = this.handleImageLayout.bind(this);
    }

    componentDidMount() {
        const { imageSource } = this.props;

        if (imageSource) {
            Image.getSize(imageSource, (width: number, height: number) => {
                this.setState({
                    imageDimWidth: width,
                    imageDimHeight: height
                });
            }, (error) => { console.log('Image getSize() error: ', error) });
        }
    }

    render() {
        const { imageSource, mode, regions, editableRegions } = this.props;

        return (
            <View style={styles.imageContainer}>

                <ReactNativeZoomableView
                    maxZoom={2.5}
                    minZoom={1}
                    zoomStep={0.5}
                    initialZoom={this.state.zoomLevel}
                    bindToBorders={true}
                    zoomEnabled={this.state.enableZoom}
                >                  
                    <ImageBackground
                        style={[styles.image, { 
                            transform: [ 
                                { translateX: 0 }, 
                                { translateY: 0 }
                            ] 
                        }]}
                        onLayout={(event: LayoutChangeEvent) => this.handleImageLayout(event)}
                        resizeMode={'contain'}
                        source={{uri: imageSource}}>

                        <View style={[styles.canvasContainer, { width: this.state.imageWidth, height: this.state.imageHeight}]}>
                            {this.getImageWithRegionsComponent(mode, regions)}

                            {mode == 'edit' && 
                                <View style={{ width: '100%', height: '100%', backgroundColor: 'rgba(0, 0, 0, 0.7)', overflow: 'hidden', zIndex: 20 }}>                                     
                                    {this.getImageWithEditableRegionsComponent(editableRegions)}
                                </View>
                            }
                            
                        </View>

                    </ImageBackground>
                </ReactNativeZoomableView>
            </View>
        );
    }

    getImageWithRegionsComponent(mode: string, regions: Array<any>) {
        let component; 
        let enable = regions && this.state.imageWidth && this.state.imageHeight;

        if (enable) {
            
            component = regions.map((obj, ind) => {
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
                                style={[ styles.touchableContainer, 
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

    getImageWithEditableRegionsComponent(regions?: Array<any>) {
        let component; 
        let enable = regions && this.state.imageWidth && this.state.imageHeight;

        if (enable && regions) {
            component = regions.map((obj, ind) => {
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
                                positionChange={(change: boolean, position: any) => {
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
    handleImageLayout(event: LayoutChangeEvent) {
        const containerHeight = event.nativeEvent.layout.height;
        const containerWidth = event.nativeEvent.layout.width;

        Image.getSize(this.props.imageSource, (width: number, height: number) => {

            const dimWidth = width;
            const dimHeight = height;
    
            const newImageWidth = containerHeight * dimWidth / dimHeight <= containerWidth ? containerHeight * dimWidth / dimHeight : containerWidth;
            const newImageHeight = containerWidth * dimHeight / dimWidth <= containerHeight ? containerWidth * dimHeight / dimWidth : containerHeight;
    
            this.setState({
                imageWidth: newImageWidth,
                imageHeight: newImageHeight
            });
            this.forceUpdate();
        }, (error) => { console.log('Image getSize() error: ', error) });
    }

    onRegionSelected(region: any) {
        let state = this.state.selectedRegions[region.id];

        if (state != RegionState.Disabled) {
            this.setState((prevState: any) => ({
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

    onSelectionChanged(selectedCount: number) {
        if (this.props.selectionChanged) {
            this.props.selectionChanged(selectedCount);
        }
    }

    getSelectedRegions() {
        let selectedRegions = this.state.selectedRegions;
        let selected = Array<any>();
        this.props.regions.forEach((region) => {
            if (selectedRegions[region.id] == RegionState.Selected) {
                let copy = JSON.parse(JSON.stringify(region));
                selected.push(copy);
            }
        });
        return selected;
    }
};
