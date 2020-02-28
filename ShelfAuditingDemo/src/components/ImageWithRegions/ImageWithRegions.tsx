import React from 'react'
import { View, Image, ImageBackground, TouchableOpacity, LayoutChangeEvent, PanResponder, PanResponderInstance } from 'react-native'
import ReactNativeZoomableView from '@dudigital/react-native-zoomable-view/src/ReactNativeZoomableView'
import { ObjectRegion, EditableRegion } from '..'
import { RegionState, ActionType, ProductItem, TagItem, BoundingBox } from '../../models'
import { Util } from '../../../Util'
import { styles } from './ImageWithRegions.style'

interface ImageProps {
    imageSource: any;
    mode: ActionType;
    regions: Array<ProductItem>;
    editableRegions?: Array<ProductItem>;
    newTagItem?: TagItem;
    selectionChanged?: Function;
    newRegionChanged?: Function;
}
  
interface ImageState {
    imageWidth: number,
    imageHeight: number,
    selectedRegions: { [key: string]: number; },
    zoomLevel: number,
    enableZoom: boolean,

    showNewRegion: boolean,
    enableAddingNewRegion: boolean,
    newRegionBBox: BoundingBox
}

export class ImageWithRegions extends React.Component<ImageProps, ImageState>  {
    private panResponder: PanResponderInstance;
    private readonly defaultRegionSize = 50;

    constructor(props: ImageProps) {
        super(props);
        this.state = {
            imageWidth: 0,
            imageHeight: 0,
            selectedRegions: {},
            zoomLevel: 1,
            enableZoom: true,

            showNewRegion: false,
            enableAddingNewRegion: false,
            newRegionBBox: new BoundingBox(0,0, this.defaultRegionSize, this.defaultRegionSize)
        };

        this.panResponder = this.getPanResponder();

        // event handlers
        this.handleImageLayout = this.handleImageLayout.bind(this);
    }

    render() {
        const { imageSource, mode, regions, editableRegions, newTagItem } = this.props;

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
                            {this.getRegionComponents(mode, regions)}

                            {mode === ActionType.Edit && 
                                <View style={styles.editCanvasContainer}>                                     
                                    {this.getEditableRegionComponents(editableRegions)}
                                </View>
                            }

                            {mode === ActionType.Add && 
                                <View style={styles.editCanvasContainer} {...this.panResponder.panHandlers}>
                                        {this.getNewEditableRegionComponent(newTagItem)}
                                </View>
                            }
                        </View>

                    </ImageBackground>
                </ReactNativeZoomableView>
            </View>
        );
    }

    getRegionComponents(mode: ActionType, regions: Array<ProductItem>) {
        let component; 
        const enable = regions && this.state.imageWidth && this.state.imageHeight;

        if (enable) {
            component = regions.map((obj: ProductItem) => {
                const model = obj.model;
                const imageWidth = this.state.imageWidth;
                const imageHeight = this.state.imageHeight;

                const bBox = new BoundingBox(
                    model.boundingBox.left * imageWidth,
                    model.boundingBox.top * imageHeight,
                    model.boundingBox.width * imageWidth,
                    model.boundingBox.height * imageHeight
                );
                
                const state = (mode === ActionType.Add || mode === ActionType.Edit) ? RegionState.Disabled : this.state.selectedRegions[obj.id];
                return <TouchableOpacity key={obj.id} onPress={() => this.onRegionSelected(obj)} activeOpacity={0.6}
                            style={[ styles.touchableContainer, { left: bBox.left, top: bBox.top, width: bBox.width, height: bBox.height, 
                                zIndex: state === RegionState.Selected ? 10 : 5
                            }]}>
                                
                            <ObjectRegion 
                                position={bBox} 
                                data={{
                                    color: Util.GetObjectRegionColor(model),
                                    state: state, 
                                    title: obj.displayName,
                                    product: obj,
                                }}
                            />
                        </TouchableOpacity>;                      
            });
        }

        return component;
    }

    getEditableRegionComponents(regions?: Array<ProductItem>) {
        let component; 
        const enable = regions && this.state.imageWidth && this.state.imageHeight;

        if (enable && regions) {
            component = regions.map((obj : ProductItem) => {
                const model = obj.model;
                const imageWidth = this.state.imageWidth;
                const imageHeight = this.state.imageHeight;

                const bBox = new BoundingBox(
                    model.boundingBox.left * imageWidth,
                    model.boundingBox.top * imageHeight,
                    model.boundingBox.width * imageWidth,
                    model.boundingBox.height * imageHeight
                );

                return <EditableRegion key={obj.id}
                            position={bBox} 
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
            });
        }

        return component;
    }

    getNewEditableRegionComponent(tagItem?: TagItem) {
        if (this.state.showNewRegion && tagItem) {
            const imageWidth = this.state.imageWidth;
            const imageHeight = this.state.imageHeight;

            const bBox = new BoundingBox(
                this.state.newRegionBBox.left * imageWidth,
                this.state.newRegionBBox.top * imageHeight,
                this.state.newRegionBBox.width * imageWidth, 
                this.state.newRegionBBox.height * imageHeight
            );

            return <EditableRegion 
                        position={bBox} 
                        data={{
                            title: tagItem.name,
                            product: null,
                        }}
                        positionChange={(change: boolean, position: any) => {
                            const bBox = new BoundingBox(
                                position.left / imageWidth, 
                                position.top / imageHeight,
                                position.width / imageWidth,
                                position.height / imageHeight);

                            this.setState({
                                enableZoom: !change,
                                newRegionBBox: bBox
                            });

                            this.onNewRegionChanged(bBox);
                        }}
                    />;
        }
    }

    // event handlers
    handleImageLayout(event: LayoutChangeEvent) {
        const containerHeight = event.nativeEvent.layout.height;
        const containerWidth = event.nativeEvent.layout.width;

        Image.getSize(this.props.imageSource, (width: number, height: number) => {
    
            const newImageWidth = containerHeight * width / height <= containerWidth ? containerHeight * width / height : containerWidth;
            const newImageHeight = containerWidth * height / width <= containerHeight ? containerWidth * height / width : containerHeight;
    
            this.setState({
                imageWidth: newImageWidth,
                imageHeight: newImageHeight
            });
            this.forceUpdate();
        }, (error) => { console.log('Image getSize() error: ', error) });
    }

    onRegionSelected(region: ProductItem) {
        const state = this.state.selectedRegions[region.id];

        if (state != RegionState.Disabled) {
            this.setState((prevState: ImageState) => ({
                selectedRegions : {
                    ...prevState.selectedRegions,
                    [region.id]: state === RegionState.Selected ? RegionState.Active : RegionState.Selected
                }
            }));

            let selectedCount = state === RegionState.Selected ? 0 : 1;
            for (const [key, value] of Object.entries(this.state.selectedRegions)) {
                if (region.id != key && value === RegionState.Selected) {
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

    onNewRegionChanged(bBox: BoundingBox) {
        if (this.props.newRegionChanged) {
            this.props.newRegionChanged(bBox);
        }
    }

    getSelectedRegions() {
        let selectedRegions = this.state.selectedRegions;
        let selected = Array<ProductItem>();
        this.props.regions.forEach((region) => {
            if (selectedRegions[region.id] === RegionState.Selected) {
                let copy = JSON.parse(JSON.stringify(region));
                selected.push(copy);
            }
        });
        return selected;
    }

    getPanResponder() {
        return PanResponder.create({
            onStartShouldSetPanResponder: (event, gestureState) => { return !this.state.enableAddingNewRegion;  },
            onMoveShouldSetPanResponder: (event, gestureState) => { return !this.state.enableAddingNewRegion;  },
            onPanResponderGrant: (event, gestureState) => { 
                const imageWidth = this.state.imageWidth;
                const imageHeight = this.state.imageHeight;
                const elem = event.nativeEvent;
                let touchX = elem.locationX;
                let touchY = elem.locationY;

                if (imageWidth > 0 && imageHeight > 0) {
                    touchX = touchX + this.defaultRegionSize <= imageWidth ? touchX : imageWidth - this.defaultRegionSize;
                    touchY = touchY + this.defaultRegionSize <= imageHeight ? touchY : imageHeight - this.defaultRegionSize;
                }
                
                const bBox = new BoundingBox(
                    touchX / imageWidth, 
                    touchY / imageHeight, 
                    this.defaultRegionSize / imageWidth, 
                    this.defaultRegionSize / imageHeight);

                this.setState({
                    showNewRegion: true,
                    enableAddingNewRegion: true,
                    newRegionBBox: bBox
                });

                this.onNewRegionChanged(bBox);
            }
        });
    }
};
