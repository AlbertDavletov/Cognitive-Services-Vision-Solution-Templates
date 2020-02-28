import React from 'react'
import { View, Image, Text, TouchableOpacity } from 'react-native'
import { NavigationScreenProp, NavigationState, NavigationParams } from 'react-navigation'
import { ImageWithRegions } from '../../components'
import { ActionType, ProductItem, TagItem, SpecData, PredictionModel, BoundingBox } from '../../models'
import { styles } from './AddEditScreen.style'
import { UnknownProduct, ShelfGap } from '../../../constants'

interface AddEditScreenProps {
    navigation: NavigationScreenProp<NavigationState, NavigationParams>;
}

interface AddEditScreenState {
    data: Array<ProductItem>,
    selectedRegions: Array<ProductItem>;
    selectedTag?: TagItem;
    newProductItem?: ProductItem;
}

export class AddEditScreen extends React.Component<AddEditScreenProps, AddEditScreenState> {
    static navigationOptions = ( { navigation } : { navigation : NavigationScreenProp<NavigationState,NavigationParams> }) => {
        const { params } = navigation.state;
        let title = "Add/Edit item";
        if (params?.mode) {
            title = params.mode === ActionType.Add ? "Add item" : "Edit item";
        }

        return { 
            title: title,
            headerRight: () => (
                <TouchableOpacity 
                    activeOpacity={0.6} 
                    onPress={() => { 
                        if (params?.applyChanges) {
                            params.applyChanges(navigation);
                        }
                    }}
                    style={{ padding: 4, marginRight: 10 }}>
                    <Text style={{color: 'white', fontSize: 16, fontWeight: 'bold' }}>Apply</Text>
                </TouchableOpacity>
            )
        }
    }

    private mode: ActionType;
    private defaultTag?: TagItem;
    private specData: SpecData;
    private imageSource: any;
    private tagCollection: Array<TagItem>;

    constructor(props: AddEditScreenProps) {
        super(props);  
        const { navigation } = this.props;
        navigation.setParams({ applyChanges: this.applyChanges });

        this.mode = navigation.getParam('mode', ActionType.Edit);
        this.specData = navigation.getParam('specData', {});
        this.imageSource = navigation.getParam('imageSource', {});

        const data = navigation.getParam('data', Array<ProductItem>());
        const selectedRegions = navigation.getParam('selectedRegions', Array<ProductItem>());

        const tags = navigation.getParam('tags', []);
        this.tagCollection = this.getTagCollection(tags, this.specData.CanonicalImages);
        this.defaultTag = this.getDefaultTag(this.tagCollection, UnknownProduct);
        
        this.state = {
            data: data,
            selectedRegions: selectedRegions
        };
    }

    componentDidMount() {
        switch (this.mode) {
            case ActionType.Add:
                if (this.defaultTag) {
                    const bbox = new BoundingBox(0, 0, 10, 10);
                    const model = new PredictionModel(this.defaultTag.id, this.defaultTag.name, bbox, 1.0);
                    this.setState({ 
                        selectedTag: this.defaultTag,
                        newProductItem: new ProductItem(model) 
                    });
                }
                break;

            case ActionType.Edit:
                if (this.state.selectedRegions.length > 0) {
                    const model = this.state.selectedRegions[0].model;
                    const tagImageUrl = this.specData.CanonicalImages + model.tagName.toLocaleLowerCase() + '.jpg';        
                    this.setState({ selectedTag: new TagItem(model.tagId, model.tagName, tagImageUrl) });
                }
                break;
        }
    }

    render() {
        let imageWithRegionsComponent;
        if (this.imageSource && this.state.data) {
            imageWithRegionsComponent = (
                <ImageWithRegions ref='editableImageWithRegions'
                    mode={this.mode}
                    imageSource={this.imageSource}
                    regions={this.state.data}
                    editableRegions={this.state.selectedRegions}
                    newTagItem={this.state.selectedTag}
                    newRegionChanged={(bBox: BoundingBox) => this.onNewRegionChanged(bBox)}
                />
            );
        }

        let selectedTagComponent;
        if (this.state.selectedTag) {
            const tag = this.state.selectedTag;
            const isUnknownProduct = tag.name.toLocaleLowerCase() === UnknownProduct.toLocaleLowerCase();
            const isShelfGap = tag.name.toLocaleLowerCase() === ShelfGap.toLocaleLowerCase();

            selectedTagComponent = 
                <View style={{ flexDirection: 'row'}}>
                    <View style={{ padding: 10 }}>
                        { !isUnknownProduct && !isShelfGap &&
                            <Image style={styles.imageThumbnail} source={{ uri: tag.imageUrl }} />
                        }
                        { isUnknownProduct &&
                            <Image style={styles.imageThumbnail} source={require('../../assets/product.jpg')} />
                        }
                        { isShelfGap &&
                            <Image style={styles.imageThumbnail} source={require('../../assets/gap.jpg')} />
                        }
                    </View>
                    
                    <View style={{ flex: 1 }}>
                        <View style={styles.tagContainer}>
                            <Text numberOfLines={1} style={styles.tagLabel}>{tag.name}</Text>
                        </View>
                        
                        <View style={styles.line}/>
                    </View>
                </View>
        } else {
            selectedTagComponent = 
                <View>
                    <Text style={[styles.tagLabel, { textAlign: 'center' }]}>Tap image to create new item</Text>
                </View>
        }

        return (
            <View style={styles.mainContainer}>

                { imageWithRegionsComponent }
                
                <View style={styles.labelContainer}>
                    <TouchableOpacity activeOpacity={0.6} onPress={() => this.onChooseLabel()} style={styles.button}>
                        <Text style={styles.buttonLabel}>Choose new label</Text>
                    </TouchableOpacity>

                    { selectedTagComponent }
                </View>
            </View>
        );
    }

    getTagCollection(tags: Array<any>, canonicalImageBaseUrl: string) {
        let tagCollection = Array<TagItem>();
        tags.forEach(t => {
            const tagImageUrl = canonicalImageBaseUrl + t.name.toLocaleLowerCase() + '.jpg';
            tagCollection.push(new TagItem(t.id, t.name, tagImageUrl));
        });

        return tagCollection;
    }

    getDefaultTag(tagCollection: Array<TagItem>, defaultTag: string) {
        if (tagCollection && tagCollection.length > 0) {
            const tag = tagCollection.filter((val) => {
                if (val.name.toLocaleLowerCase() === defaultTag.toLocaleLowerCase()) {
                    return val;
                }
            });
    
            return tag && tag.length > 0 ? tag[0] : tagCollection[0];
        }
    }

    onChooseLabel() {
        const { navigate } = this.props.navigation;
        navigate('TagCollection', { tags: this.tagCollection, returnData: this.returnData.bind(this) });
    }

    onNewRegionChanged(bBox: BoundingBox) {
        const { navigation } = this.props;
        const tag = this.state.selectedTag;

        if (bBox && tag && this.state.newProductItem) {
            const model = new PredictionModel(tag.id, tag.name, bBox, 1.0);
            const productItem = new ProductItem(model);

            this.setState({ newProductItem: productItem });
            navigation.setParams({ newProductItem: productItem });
        }
    }

    returnData(selectedTag: any) {
        const { navigation } = this.props;

        this.state.selectedRegions.forEach(r => {
            r.displayName = selectedTag.name,
            r.model.tagId = selectedTag.id,
            r.model.tagName = selectedTag.name
        });

        let productItem;
        if (this.state.newProductItem) {
            const box = this.state.newProductItem.model.boundingBox;
            const model = new PredictionModel(selectedTag.id, selectedTag.name, box, 1.0);
            productItem = new ProductItem(model);
            navigation.setParams({ newProductItem: productItem });
        }

        this.setState({ 
            selectedTag: selectedTag, 
            newProductItem: productItem
        });
    }

    applyChanges(navigation: NavigationScreenProp<NavigationState, NavigationParams>) {
        const { params } = navigation.state;
        if (params) {
            switch (params.mode) {
                case ActionType.Add:
                    if (params.addCallback) {
                        params.addCallback(params.newProductItem);
                    }
                    break;

                case ActionType.Edit:
                    if (params.editCallback) {
                        params.editCallback(params.selectedRegions);
                    }
                    break;
            }
        }
        
        navigation.goBack();
    }
}
